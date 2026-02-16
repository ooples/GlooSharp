/*
 * gloo_native.h — C shim header for Gloo C++ collective communications library.
 *
 * Gloo is a C++ library, but P/Invoke requires C linkage (extern "C").
 * This header declares the thin C wrapper that translates between C-style calls
 * and the underlying Gloo C++ API.
 *
 * All functions return an integer error code (0 = success).
 * Opaque pointers (void*) are used for Gloo objects (Context, etc.).
 *
 * Build: See CMakeLists.txt in this directory.
 *
 * Copyright (c) 2026 Ooples Finance LLC. Licensed under Apache-2.0.
 */

#ifndef GLOO_NATIVE_H
#define GLOO_NATIVE_H

#ifdef __cplusplus
extern "C" {
#endif

/* ─── Platform export macro ─────────────────────────────────────── */

#if defined(_WIN32) || defined(_WIN64)
    #ifdef GLOO_NATIVE_EXPORTS
        #define GLOO_NATIVE_EXPORT __declspec(dllexport)
    #else
        #define GLOO_NATIVE_EXPORT __declspec(dllimport)
    #endif
#else
    #define GLOO_NATIVE_EXPORT __attribute__((visibility("default")))
#endif

/* ─── Error codes ───────────────────────────────────────────────── */

#define GLOO_SUCCESS            0
#define GLOO_INVALID_ARGUMENT   1
#define GLOO_SYSTEM_ERROR       2
#define GLOO_TRANSPORT_ERROR    3
#define GLOO_TIMEOUT            4
#define GLOO_NOT_INITIALIZED    5
#define GLOO_INTERNAL_ERROR     6

/* ─── Data types ────────────────────────────────────────────────── */

#define GLOO_FLOAT32  0
#define GLOO_FLOAT64  1
#define GLOO_INT32    2
#define GLOO_INT64    3

/* ─── Reduction operations ──────────────────────────────────────── */

#define GLOO_OP_SUM     0
#define GLOO_OP_PRODUCT 1
#define GLOO_OP_MIN     2
#define GLOO_OP_MAX     3

/* ─── Context lifecycle ─────────────────────────────────────────── */

/**
 * Creates a new Gloo context for the given rank and world size.
 *
 * @param rank      This process's rank (0-based).
 * @param size      Total number of processes.
 * @param ctx_out   [out] Receives the opaque context pointer.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_context_create(int rank, int size, void** ctx_out);

/**
 * Destroys a Gloo context and frees all associated resources.
 *
 * @param ctx       The context to destroy.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_context_destroy(void* ctx);

/* ─── Transport ─────────────────────────────────────────────────── */

/**
 * Creates a TCP transport and attaches it to the context.
 * Uses a FileStore at store_path for rendezvous, then calls connectFullMesh
 * to establish all-to-all connections between processes.
 *
 * @param ctx        The Gloo context.
 * @param hostname   Hostname or IP to bind to.
 * @param port       Base port number.
 * @param store_path Path to a shared directory for rendezvous (FileStore).
 *                   All processes must be able to read/write this path.
 * @return           GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_transport_tcp_create(void* ctx, const char* hostname,
                                                  int port, const char* store_path);

/**
 * Creates an InfiniBand transport and attaches it to the context.
 * Uses a FileStore at store_path for rendezvous, then calls connectFullMesh
 * to establish all-to-all connections between processes.
 *
 * @param ctx        The Gloo context.
 * @param device     IB device name (e.g., "mlx5_0"). Empty string for auto-detect.
 * @param store_path Path to a shared directory for rendezvous (FileStore).
 * @return           GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_transport_ib_create(void* ctx, const char* device,
                                                 const char* store_path);

/**
 * Checks whether InfiniBand hardware is available.
 *
 * @return          GLOO_SUCCESS if IB is available, GLOO_TRANSPORT_ERROR otherwise.
 */
GLOO_NATIVE_EXPORT int gloo_transport_ib_available(void);

/* ─── Collective operations ─────────────────────────────────────── */

/**
 * AllReduce: Combines data from all processes and distributes the result to all.
 *
 * @param ctx       The Gloo context.
 * @param sendbuf   Pointer to the send buffer.
 * @param recvbuf   Pointer to the receive buffer (may be same as sendbuf for in-place).
 * @param count     Number of elements.
 * @param datatype  Data type code (GLOO_FLOAT32, etc.).
 * @param op        Reduction operation (GLOO_OP_SUM, etc.).
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_allreduce(void* ctx, void* sendbuf, void* recvbuf,
                                       int count, int datatype, int op);

/**
 * Broadcast: Sends data from root to all other processes.
 *
 * @param ctx       The Gloo context.
 * @param buf       Pointer to the data buffer (in-place).
 * @param count     Number of elements.
 * @param datatype  Data type code.
 * @param root      Rank of the broadcasting process.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_broadcast(void* ctx, void* buf, int count,
                                       int datatype, int root);

/**
 * AllGather: Gathers data from all processes into a single buffer on each.
 *
 * @param ctx       The Gloo context.
 * @param sendbuf   Pointer to this process's send data.
 * @param sendcount Number of elements in sendbuf.
 * @param recvbuf   Pointer to receive buffer (size = sendcount * worldSize).
 * @param datatype  Data type code.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_allgather(void* ctx, void* sendbuf, int sendcount,
                                       void* recvbuf, int datatype);

/**
 * Barrier: Blocks until all processes have reached the barrier.
 *
 * @param ctx       The Gloo context.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_barrier(void* ctx);

/**
 * ReduceScatter: Reduces data and scatters the result among processes.
 *
 * @param ctx       The Gloo context.
 * @param sendbuf   Pointer to the send buffer.
 * @param recvbuf   Pointer to the receive buffer (size = count / worldSize).
 * @param count     Total number of elements in sendbuf.
 * @param datatype  Data type code.
 * @param op        Reduction operation.
 * @return          GLOO_SUCCESS or an error code.
 */
GLOO_NATIVE_EXPORT int gloo_reduce_scatter(void* ctx, void* sendbuf, void* recvbuf,
                                            int count, int datatype, int op);

#ifdef __cplusplus
}
#endif

#endif /* GLOO_NATIVE_H */
