export interface RoleDto {
    id: string
    name: string
    description: string
    permissions: string[]
}

export interface CreateRoleRequest {
    name: string
    description: string
    permissions: string[]
}

export interface UpdateRoleRequest extends CreateRoleRequest {
    id: string
}