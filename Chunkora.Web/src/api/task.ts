import {api} from "@/api/client.ts";
import type {PageResult} from "@/types/api.ts";
import type {CreateInstanceTaskRequest, InstanceTaskDto, UpdateInstanceTaskRequest} from "@/types/task";

export function createInstanceTask(data: CreateInstanceTaskRequest) {
    return api<InstanceTaskDto>(`/tasks`, {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function updateInstanceTask(data: UpdateInstanceTaskRequest) {
    return api<InstanceTaskDto>(`/tasks`, {
        method: "PUT",
        body: JSON.stringify(data),
    })
}

export function getInstanceTask(instanceId: string, id: string) {
    return api<InstanceTaskDto>(`/tasks/${instanceId}/${id}`, {
        method: "GET"
    })
}

export function deleteInstanceTask(instanceId: string, id: string) {
    return api<InstanceTaskDto>(`/tasks/${instanceId}/${id}`, {
        method: "DELETE"
    })
}

export function listInstanceTasks(instanceId: string, offset: number, limit: number) {
    return api<PageResult<InstanceTaskDto>>(`/tasks/${instanceId}?offset=${offset}&limit=${limit}`, {
        method: "GET"
    })
}