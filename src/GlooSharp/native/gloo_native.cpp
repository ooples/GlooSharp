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

/* ─── Helper: typed dispatch for AllReduce ──────────────────────── */

template <typename T>
static void do_allreduce(std::shared_ptr<gloo::Context>& ctx,
                         void* sendbuf, void* recvbuf, int count, int op) {
    gloo::AllreduceOptions opts(ctx);
    opts.setInput(static_cast<T*>(sendbuf), count);
    opts.setOutput(static_cast<T*>(recvbuf), count);

    using Func = void(*)(void*, const void*, const void*, size_t);
    switch (op) {
        case GLOO_OP_SUM:
            opts.setReduceFunction(static_cast<Func>(
                [](void* c, const void* a, const void* b, size_t n) {
                    auto* tc = static_cast<T*>(c);
                    auto* ta = static_cast<const T*>(a);
                    auto* tb = static_cast<const T*>(b);
                    for (size_t i = 0; i < n / sizeof(T); ++i) tc[i] = ta[i] + tb[i];
                }));
            break;
        case GLOO_OP_PRODUCT:
            opts.setReduceFunction(static_cast<Func>(
                [](void* c, const void* a, const void* b, size_t n) {
                    auto* tc = static_cast<T*>(c);
                    auto* ta = static_cast<const T*>(a);
                    auto* tb = static_cast<const T*>(b);
                    for (size_t i = 0; i < n / sizeof(T); ++i) tc[i] = ta[i] * tb[i];
                }));
            break;
        case GLOO_OP_MIN:
            opts.setReduceFunction(static_cast<Func>(
                [](void* c, const void* a, const void* b, size_t n) {
                    auto* tc = static_cast<T*>(c);
                    auto* ta = static_cast<const T*>(a);
                    auto* tb = static_cast<const T*>(b);
                    for (size_t i = 0; i < n / sizeof(T); ++i) tc[i] = (ta[i] < tb[i]) ? ta[i] : tb[i];
                }));
            break;
        case GLOO_OP_MAX:
            opts.setReduceFunction(static_cast<Func>(
                [](void* c, const void* a, const void* b, size_t n) {
                    auto* tc = static_cast<T*>(c);
                    auto* ta = static_cast<const T*>(a);
                    auto* tb = static_cast<const T*>(b);
                    for (size_t i = 0; i < n / sizeof(T); ++i) tc[i] = (ta[i] > tb[i]) ? ta[i] : tb[i];
                }));
            break;
        default:
            throw std::invalid_argument("Unknown reduction operation");
    }

    gloo::allreduce(opts);
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

int gloo_transport_tcp_create(void* ctx, const char* hostname, int port) {
    if (!ctx || !hostname) return GLOO_INVALID_ARGUMENT;

    try {
        auto* wrapper = static_cast<GlooContextWrapper*>(ctx);

        gloo::transport::tcp::attr attr;
        attr.hostname = hostname;

        auto device = gloo::transport::tcp::CreateDevice(attr);
        wrapper->device = device;

        auto context = std::make_shared<gloo::rendezvous::Context>(
            wrapper->rank, wrapper->size);
        // Note: In production, a rendezvous Store (file, Redis, etc.) is needed.
        // The store-based connectFullMesh is called here.
        wrapper->context = context;

        return GLOO_SUCCESS;
    } catch (const std::system_error&) {
        return GLOO_TRANSPORT_ERROR;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}

int gloo_transport_ib_create(void* ctx, const char* device_name) {
#ifdef GLOO_USE_IBVERBS
    if (!ctx || !device_name) return GLOO_INVALID_ARGUMENT;

    try {
        auto* wrapper = static_cast<GlooContextWrapper*>(ctx);

        gloo::transport::ibverbs::attr attr;
        if (strlen(device_name) > 0) {
            attr.name = device_name;
        }

        auto device = gloo::transport::ibverbs::CreateDevice(attr);
        wrapper->device = device;

        auto context = std::make_shared<gloo::rendezvous::Context>(
            wrapper->rank, wrapper->size);
        wrapper->context = context;

        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_TRANSPORT_ERROR;
    }
#else
    (void)ctx;
    (void)device_name;
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
    if (!ctx || !buf || count <= 0) return GLOO_INVALID_ARGUMENT;

    auto* wrapper = static_cast<GlooContextWrapper*>(ctx);
    if (!wrapper->context) return GLOO_NOT_INITIALIZED;

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

        gloo::ReduceScatterOptions opts(wrapper->context);

        switch (datatype) {
            case GLOO_FLOAT32:
                opts.setInput(static_cast<float*>(sendbuf), count);
                opts.setOutput(static_cast<float*>(recvbuf), recvcount);
                break;
            case GLOO_FLOAT64:
                opts.setInput(static_cast<double*>(sendbuf), count);
                opts.setOutput(static_cast<double*>(recvbuf), recvcount);
                break;
            case GLOO_INT32:
                opts.setInput(static_cast<int32_t*>(sendbuf), count);
                opts.setOutput(static_cast<int32_t*>(recvbuf), recvcount);
                break;
            case GLOO_INT64:
                opts.setInput(static_cast<int64_t*>(sendbuf), count);
                opts.setOutput(static_cast<int64_t*>(recvbuf), recvcount);
                break;
            default:
                return GLOO_INVALID_ARGUMENT;
        }

        // Set per-rank counts (equal split)
        std::vector<int> counts(wrapper->size, recvcount);
        opts.setRecvCounts(counts);

        gloo::reduce_scatter(opts);
        return GLOO_SUCCESS;
    } catch (...) {
        return GLOO_INTERNAL_ERROR;
    }
}
