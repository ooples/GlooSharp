/*
 * gloo_native.cpp — C shim implementation wrapping the Gloo C++ library.
 *
 * This file provides extern "C" functions that:
 *   1. Manage Gloo objects (Context, Transport) as opaque void* pointers.
 *   2. Catch all C++ exceptions and convert them to integer error codes.
 *   3. Dispatch to the appropriate gloo:: template functions based on datatype.
 *
 * Build requirements:
 *   - Gloo library (libgloo) installed and discoverable by CMake.
 *   - Optional: libibverbs for InfiniBand support.
 *
 * Copyright (c) 2026 Ooples Finance LLC. Licensed under Apache-2.0.
 */

#include "gloo_native.h"

#include <gloo/context.h>
#include <gloo/rendezvous/context.h>
#include <gloo/rendezvous/file_store.h>
#include <gloo/transport/tcp/device.h>
#include <gloo/allreduce.h>
#include <gloo/broadcast.h>
#include <gloo/allgather.h>
#include <gloo/barrier.h>
#include <gloo/reduce_scatter.h>

#ifdef GLOO_USE_IBVERBS
#include <gloo/transport/ibverbs/device.h>
#endif

#include <memory>
#include <stdexcept>
#include <cstring>
#include <string>
#include <vector>

/* ─── Internal wrapper struct ───────────────────────────────────── */

struct GlooContextWrapper {
    std::shared_ptr<gloo::Context> context;
    std::shared_ptr<gloo::transport::Device> device;
    int rank;
    int size;
};

/* ─── Helper: run a callback and catch all exceptions ───────────── */

static int safe_call(int (*fn)(GlooContextWrapper*), void* ctx) {
    if (!ctx) return GLOO_INVALID_ARGUMENT;
    try {
        return fn(static_cast<GlooContextWrapper*>(ctx));
    } catch (const std::invalid_argument&) {
        return GLOO_INVALID_ARGUMENT;
    } catch (const std::system_error&) {
        return GLOO_SYSTEM_ERROR;
    } catch (const std::runtime_error&) {
        return GLOO_TRANSPORT_ERROR;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

/* ─── Helper: get element size from datatype code ───────────────── */

static size_t element_size(int datatype) {
    switch (datatype) {
        case GLOO_FLOAT32: return sizeof(float);
        case GLOO_FLOAT64: return sizeof(double);
        case GLOO_INT32:   return sizeof(int32_t);
        case GLOO_INT64:   return sizeof(int64_t);
        default:           return 0;
    }
}

/* ─── Helper: get typed reduce function for a given op ────────────── */

using ReduceFunc = void(*)(void*, const void*, const void*, size_t);

template <typename T>
static ReduceFunc get_reduce_function(int op) {
    switch (op) {
        case GLOO_OP_SUM:
            return [](void* c, const void* a, const void* b, size_t n) {
                auto* tc = static_cast<T*>(c);
                auto* ta = static_cast<const T*>(a);
                auto* tb = static_cast<const T*>(b);
                for (size_t i = 0; i < n; ++i) tc[i] = ta[i] + tb[i];
            };
        case GLOO_OP_PRODUCT:
            return [](void* c, const void* a, const void* b, size_t n) {
                auto* tc = static_cast<T*>(c);
                auto* ta = static_cast<const T*>(a);
                auto* tb = static_cast<const T*>(b);
                for (size_t i = 0; i < n; ++i) tc[i] = ta[i] * tb[i];
            };
        case GLOO_OP_MIN:
            return [](void* c, const void* a, const void* b, size_t n) {
                auto* tc = static_cast<T*>(c);
                auto* ta = static_cast<const T*>(a);
                auto* tb = static_cast<const T*>(b);
                for (size_t i = 0; i < n; ++i) tc[i] = (ta[i] < tb[i]) ? ta[i] : tb[i];
            };
        case GLOO_OP_MAX:
            return [](void* c, const void* a, const void* b, size_t n) {
                auto* tc = static_cast<T*>(c);
                auto* ta = static_cast<const T*>(a);
                auto* tb = static_cast<const T*>(b);
                for (size_t i = 0; i < n; ++i) tc[i] = (ta[i] > tb[i]) ? ta[i] : tb[i];
            };
        default:
            throw std::invalid_argument("Unknown reduction operation");
    }
}

/* ─── Helper: typed dispatch for AllReduce ──────────────────────── */

template <typename T>
static void do_allreduce(std::shared_ptr<gloo::Context>& ctx,
                         void* sendbuf, void* recvbuf, int count, int op) {
    gloo::AllreduceOptions opts(ctx);
    opts.setInput(static_cast<T*>(sendbuf), count);
    opts.setOutput(static_cast<T*>(recvbuf), count);
    opts.setReduceFunction(static_cast<ReduceFunc>(get_reduce_function<T>(op)));
    gloo::allreduce(opts);
}

/* ─── Helper: typed dispatch for ReduceScatter ─────────────────── */

template <typename T>
static void do_reduce_scatter(std::shared_ptr<gloo::Context>& ctx,
                               void* sendbuf, void* recvbuf,
                               int count, int recvcount, int worldSize, int op) {
    gloo::ReduceScatterOptions opts(ctx);
    opts.setInput(static_cast<T*>(sendbuf), count);
    opts.setOutput(static_cast<T*>(recvbuf), recvcount);
    opts.setReduceFunction(static_cast<ReduceFunc>(get_reduce_function<T>(op)));

    std::vector<int> counts(worldSize, recvcount);
    opts.setRecvCounts(counts);

    gloo::reduce_scatter(opts);
}

/* ─── Context lifecycle ─────────────────────────────────────────── */

int gloo_context_create(int rank, int size, void** ctx_out) {
    if (!ctx_out || rank < 0 || size < 1 || rank >= size)
        return GLOO_INVALID_ARGUMENT;

    try {
        auto wrapper = new GlooContextWrapper();
        wrapper->rank = rank;
        wrapper->size = size;
        // Context is created after transport is attached
        *ctx_out = wrapper;
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_SYSTEM_ERROR;
    }
}

int gloo_context_destroy(void* ctx) {
    if (!ctx) return GLOO_INVALID_ARGUMENT;
    try {
        delete static_cast<GlooContextWrapper*>(ctx);
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

/* ─── Transport ─────────────────────────────────────────────────── */

int gloo_transport_tcp_create(void* ctx, const char* hostname,
                              int port, const char* store_path) {
    if (!ctx || !hostname || !store_path) return GLOO_INVALID_ARGUMENT;

    try {
        auto* wrapper = static_cast<GlooContextWrapper*>(ctx);

        // Note: Gloo's tcp::attr does not have a port field. Gloo binds to an
        // ephemeral port and uses the rendezvous store for address exchange.
        // The port parameter is kept in the API for user-facing clarity but is
        // not used by the transport layer.
        (void)port;

        gloo::transport::tcp::attr attr;
        attr.hostname = hostname;

        auto device = gloo::transport::tcp::CreateDevice(attr);
        wrapper->device = device;

        // Create rendezvous FileStore — all processes must share this directory
        auto store = std::make_shared<gloo::rendezvous::FileStore>(
            std::string(store_path));

        // Create rendezvous context and connect all processes
        auto context = std::make_shared<gloo::rendezvous::Context>(
            wrapper->rank, wrapper->size);
        context->connectFullMesh(store, device);
        wrapper->context = context;

        return GLOO_SUCCESS;
    } catch (const std::system_error&) {
        return GLOO_TRANSPORT_ERROR;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_transport_ib_create(void* ctx, const char* device_name,
                             const char* store_path) {
#ifdef GLOO_USE_IBVERBS
    if (!ctx || !device_name || !store_path) return GLOO_INVALID_ARGUMENT;

    try {
        auto* wrapper = static_cast<GlooContextWrapper*>(ctx);

        gloo::transport::ibverbs::attr attr;
        if (strlen(device_name) > 0) {
            attr.name = device_name;
        }

        auto device = gloo::transport::ibverbs::CreateDevice(attr);
        wrapper->device = device;

        // Create rendezvous FileStore — all processes must share this directory
        auto store = std::make_shared<gloo::rendezvous::FileStore>(
            std::string(store_path));

        // Create rendezvous context and connect all processes
        auto context = std::make_shared<gloo::rendezvous::Context>(
            wrapper->rank, wrapper->size);
        context->connectFullMesh(store, device);
        wrapper->context = context;

        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_TRANSPORT_ERROR;
    }
#else
    (void)ctx;
    (void)device_name;
    (void)store_path;
    return GLOO_TRANSPORT_ERROR;
#endif
}

int gloo_transport_ib_available(void) {
#ifdef GLOO_USE_IBVERBS
    try {
        gloo::transport::ibverbs::attr attr;
        auto device = gloo::transport::ibverbs::CreateDevice(attr);
        return (device != nullptr) ? GLOO_SUCCESS : GLOO_TRANSPORT_ERROR;
    } catch (...) {
        return GLOO_TRANSPORT_ERROR;
    }
#else
    return GLOO_TRANSPORT_ERROR;
#endif
}

/* ─── Collective operations ─────────────────────────────────────── */

int gloo_allreduce(void* ctx, void* sendbuf, void* recvbuf,
                   int count, int datatype, int op) {
    if (!ctx || !sendbuf || !recvbuf || count <= 0) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;

    try {
        switch (datatype) {
            case GLOO_FLOAT32:
                do_allreduce<float>(wrapper->context, sendbuf, recvbuf, count, op);
                break;
            case GLOO_FLOAT64:
                do_allreduce<double>(wrapper->context, sendbuf, recvbuf, count, op);
                break;
            case GLOO_INT32:
                do_allreduce<int32_t>(wrapper->context, sendbuf, recvbuf, count, op);
                break;
            case GLOO_INT64:
                do_allreduce<int64_t>(wrapper->context, sendbuf, recvbuf, count, op);
                break;
            default:
                return GLOO_INVALID_ARGUMENT;
        }
        return GLOO_SUCCESS;
    } catch (const std::invalid_argument&) {
        return GLOO_INVALID_ARGUMENT;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_broadcast(void* ctx, void* buf, int count, int datatype, int root) {
    if (!ctx || !buf || count <= 0 || root < 0) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;
    if (root >= wrapper->size) return GLOO_INVALID_ARGUMENT;

    try {
        size_t esize = element_size(datatype);
        if (esize == 0) return GLOO_INVALID_ARGUMENT;

        gloo::BroadcastOptions opts(wrapper->context);
        opts.setRoot(root);

        switch (datatype) {
            case GLOO_FLOAT32:
                opts.setOutput(static_cast<float*>(buf), count);
                break;
            case GLOO_FLOAT64:
                opts.setOutput(static_cast<double*>(buf), count);
                break;
            case GLOO_INT32:
                opts.setOutput(static_cast<int32_t*>(buf), count);
                break;
            case GLOO_INT64:
                opts.setOutput(static_cast<int64_t*>(buf), count);
                break;
            default:
                return GLOO_INVALID_ARGUMENT;
        }

        gloo::broadcast(opts);
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_allgather(void* ctx, void* sendbuf, int sendcount,
                   void* recvbuf, int datatype) {
    if (!ctx || !sendbuf || !recvbuf || sendcount <= 0) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;

    try {
        size_t esize = element_size(datatype);
        if (esize == 0) return GLOO_INVALID_ARGUMENT;

        gloo::AllgatherOptions opts(wrapper->context);

        switch (datatype) {
            case GLOO_FLOAT32:
                opts.setInput(static_cast<float*>(sendbuf), sendcount);
                opts.setOutput(static_cast<float*>(recvbuf), sendcount * wrapper->size);
                break;
            case GLOO_FLOAT64:
                opts.setInput(static_cast<double*>(sendbuf), sendcount);
                opts.setOutput(static_cast<double*>(recvbuf), sendcount * wrapper->size);
                break;
            case GLOO_INT32:
                opts.setInput(static_cast<int32_t*>(sendbuf), sendcount);
                opts.setOutput(static_cast<int32_t*>(recvbuf), sendcount * wrapper->size);
                break;
            case GLOO_INT64:
                opts.setInput(static_cast<int64_t*>(sendbuf), sendcount);
                opts.setOutput(static_cast<int64_t*>(recvbuf), sendcount * wrapper->size);
                break;
            default:
                return GLOO_INVALID_ARGUMENT;
        }

        gloo::allgather(opts);
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_barrier(void* ctx) {
    if (!ctx) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;

    try {
        gloo::BarrierOptions opts(wrapper->context);
        gloo::barrier(opts);
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_reduce_scatter(void* ctx, void* sendbuf, void* recvbuf,
                        int count, int datatype, int op) {
    if (!ctx || !sendbuf || !recvbuf || count <= 0) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;

    try {
        size_t esize = element_size(datatype);
        if (esize == 0) return GLOO_INVALID_ARGUMENT;

        int recvcount = count / wrapper->size;
        if (recvcount * wrapper->size != count) return GLOO_INVALID_ARGUMENT;

        switch (datatype) {
            case GLOO_FLOAT32:
                do_reduce_scatter<float>(wrapper->context, sendbuf, recvbuf,
                    count, recvcount, wrapper->size, op);
                break;
            case GLOO_FLOAT64:
                do_reduce_scatter<double>(wrapper->context, sendbuf, recvbuf,
                    count, recvcount, wrapper->size, op);
                break;
            case GLOO_INT32:
                do_reduce_scatter<int32_t>(wrapper->context, sendbuf, recvbuf,
                    count, recvcount, wrapper->size, op);
                break;
            case GLOO_INT64:
                do_reduce_scatter<int64_t>(wrapper->context, sendbuf, recvbuf,
                    count, recvcount, wrapper->size, op);
                break;
            default:
                return GLOO_INVALID_ARGUMENT;
        }
        return GLOO_SUCCESS;
    } catch (const std::invalid_argument&) {
        return GLOO_INVALID_ARGUMENT;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}
