import {api} from "@/api/client.ts";
import type {PageResult} from "@/types/api.ts";
import type {
    CreateNodeRequest,
    NodeDto,
    NodeMetricsDto,
    NodeSecureDto,
    NodeStatusDto,
    UpdateNodeRequest
} from "@/types/node.ts";

export function listNodes(offset: number, limit: number) {
    return api<PageResult<NodeDto>>(`/nodes?offset=${offset}&limit=${limit}`, {
        method: "GET"
    })
}

export function deleteNode(id: string) {
    return api<PageResult<NodeDto>>(`/nodes/${id}`, {
        method: "DELETE"
    })
}

export function createNode(data: CreateNodeRequest) {
    return api<PageResult<NodeDto>>(`/nodes`, {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function getNode(id: string) {
    return api<NodeDto>(`/nodes/${id}`, {
        method: "GET"
    })
}

export function updateNode(data: UpdateNodeRequest) {
    return api<NodeDto>(`/nodes`, {
        method: "PUT",
        body: JSON.stringify(data),
    })
}

export function getNodeSecure(id: string) {
    return api<NodeSecureDto>(`/nodes/${id}/secure`, {
        method: "GET"
    })
}

export function getNodeLatestMetrics(id: string) {
    return api<NodeMetricsDto>(`/nodes/${id}/metrics/latest`, {
        method: "GET"
    })
}

export function getNodeMetrics(id: string, from: string, to: string, offset: number, limit: number) {
    return api<PageResult<NodeMetricsDto>>(`/nodes/${id}/metrics?from=${from}&to=${to}&limit=${limit}&offset=${offset}`, {
        method: "GET",
    })
}

export function getNodeStatus(id: string) {
    return api<NodeStatusDto>(`/nodes/${id}/status`, {
        method: "GET"
    })
}