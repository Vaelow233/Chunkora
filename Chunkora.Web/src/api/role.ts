import {api} from "@/api/client.ts";
import type {CreateRoleRequest, RoleDto, UpdateRoleRequest} from "@/types/role.ts";
import type {PageResult} from "@/types/api.ts";

export function getSelfRole() {
    return api<RoleDto>("/roles/self", {
        method: "GET",
    })
}

export function listRoles(offset: number, limit: number) {
    return api<PageResult<RoleDto>>(`/roles?offset=${offset}&limit=${limit}`, {
        method: "GET"
    })
}

export function deleteRole(id: string) {
    return api<PageResult<RoleDto>>(`/roles/${id}`, {
        method: "DELETE"
    })
}

export function createRole(data: CreateRoleRequest) {
    return api<PageResult<RoleDto>>(`/roles`, {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function updateRole(data: UpdateRoleRequest) {
    return api<RoleDto>(`/roles`, {
        method: "PUT",
        body: JSON.stringify(data),
    })
}

export function getRole(id: string) {
    return api<RoleDto>(`/roles/${id}`, {
        method: "GET"
    })
}
