import {api} from "@/api/client.ts";
import {ApiError, type ApiFailure, type ApiSuccess, type PageResult} from "@/types/api.ts";
import type {
    CreateInstanceRequest,
    FileContentDto,
    FileObjectDto,
    InstanceDto,
    UpdateInstanceRequest
} from "@/types/instance.ts";
import {useUserStore} from "@/stores/auth.ts";

export function listInstances(offset: number, limit: number) {
    return api<PageResult<InstanceDto>>(`/instances?offset=${offset}&limit=${limit}`, {
        method: "GET"
    })
}

export function deleteInstance(id: string) {
    return api<PageResult<InstanceDto>>(`/instances/${id}`, {
        method: "DELETE"
    })
}

export function createInstance(data: CreateInstanceRequest) {
    return api<PageResult<InstanceDto>>(`/instances`, {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function getInstance(id: string) {
    return api<InstanceDto>(`/instances/${id}`, {
        method: "GET"
    })
}

export function updateInstance(data: UpdateInstanceRequest) {
    return api<InstanceDto>(`/instances`, {
        method: "PUT",
        body: JSON.stringify(data),
    })
}

export function startInstance(id: string) {
    return api<InstanceDto>(`/instances/${id}/start`, {
        method: "POST"
    })
}

export function stopInstance(id: string) {
    return api<InstanceDto>(`/instances/${id}/stop`, {
        method: "POST"
    })
}

export function restartInstance(id: string) {
    return api<InstanceDto>(`/instances/${id}/restart`, {
        method: "POST"
    })
}

export function killInstance(id: string) {
    return api<InstanceDto>(`/instances/${id}/kill`, {
        method: "POST"
    })
}

export function getInstanceLogs(id: string, offset: number, limit: number) {
    const params = new URLSearchParams({ "offset": offset.toString(), "limit": limit.toString() })
    return api<PageResult<string>>(`/instances/${id}/logs?${params}`, {
        method: "GET"
    })
}

export function executeCommand(id: string, command: string) {
    const params = new URLSearchParams({ command })
    return api<InstanceDto>(`/instances/${id}/execute?${params}`, {
        method: "POST"
    })
}

export function listFiles(id: string, path: string, offset: number, limit: number) {
    const params = new URLSearchParams({ "path": path, "offset": offset.toString(), "limit": limit.toString() })
    return api<PageResult<FileObjectDto>>(`/instances/${id}/files/list?${params}`, {
        method: "GET"
    })
}

export function readFile(id: string, path: string) {
    const params = new URLSearchParams({ path })
    return api<FileContentDto>(`/instances/${id}/files/read?${params}`, {
        method: "GET"
    })
}

export async function downloadFile(
    id: string,
    path: string
): Promise<void> {
    const params = new URLSearchParams({ path })

    const response = await fetch(
        `/api/instances/${id}/files/download?${params}`,
        {
            method: "GET",
            headers: {
                Authorization: `Bearer ${useUserStore.getState().accessToken}`,
            },
        }
    )

    if (!response.ok) {
        const error = await response.json()
        throw new Error(error.error ?? "Failed to download file")
    }

    const blob = await response.blob()
    const url = URL.createObjectURL(blob)

    const filename = path.replaceAll("\\", "/").split("/").pop() || "download"

    const a = document.createElement("a")
    a.href = url
    a.download = filename

    document.body.appendChild(a)
    a.click()
    a.remove()

    setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export function createNewFile(id: string, basePath: string, name: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name })
    return api<void>(`/instances/${id}/files/create?${params}`, {
        method: "POST"
    })
}

export function createNewDirectory(id: string, basePath: string, name: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name })
    return api<void>(`/instances/${id}/directories/create?${params}`, {
        method: "POST"
    })
}

export async function uploadFile(id: string, path: string, file: File) {
    const params = new URLSearchParams({ path })
    const response = await fetch(`/api/instances/${id}/files/upload?${params}`, {
        method: "PUT",
        headers: {
            "Authorization": `Bearer ${useUserStore.getState().accessToken}`,
            "Content-Type": "application/octet-stream",
        },
        body: file
    })
    const body = await response.json() as | ApiSuccess<void> | ApiFailure;
    if (!response.ok || !body.isSuccess) {
        const failure = body as ApiFailure;
        throw new ApiError(
            response.status,
            failure.error.code,
            failure.error.identifier,
            failure.error.message
        );
    }
    return (body as ApiSuccess<void>).data;
}

export function deleteFile(id: string, basePath: string, name: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name })
    return api<void>(`/instances/${id}/files/delete?${params}`, {
        method: "DELETE"
    })
}

export function deleteDirectory(id: string, basePath: string, name: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name })
    return api<void>(`/instances/${id}/directories/delete?${params}`, {
        method: "DELETE"
    })
}

export function copyFile(id: string, basePath: string, name: string, destination: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "destination": destination })
    return api<void>(`/instances/${id}/files/copy?${params}`, {
        method: "POST"
    })
}

export function copyDirectory(id: string, basePath: string, name: string, destination: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "destination": destination })
    return api<void>(`/instances/${id}/directories/copy?${params}`, {
        method: "POST"
    })
}

export function moveFile(id: string, basePath: string, name: string, destination: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "destination": destination })
    return api<void>(`/instances/${id}/files/move?${params}`, {
        method: "POST"
    })
}

export function moveDirectory(id: string, basePath: string, name: string, destination: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "destination": destination })
    return api<void>(`/instances/${id}/directories/move?${params}`, {
        method: "POST"
    })
}

export function renameFile(id: string, basePath: string, name: string, newName: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "newName": newName })
    return api<void>(`/instances/${id}/files/rename?${params}`, {
        method: "POST"
    })
}

export function renameDirectory(id: string, basePath: string, name: string, newName: string) {
    const params = new URLSearchParams({ "basePath": basePath, "name": name, "newName": newName })
    return api<void>(`/instances/${id}/directories/rename?${params}`, {
        method: "POST"
    })
}