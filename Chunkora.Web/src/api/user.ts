import {api} from "@/api/client.ts";
import type {
    CreateUserRequest,
    LoginRequest,
    RefreshTokenRequest,
    UpdateUserRequest,
    UserDto,
    UserSecureDto
} from "@/types/user.ts";
import type {PageResult} from "@/types/api.ts";

export function login(data: LoginRequest) {
    return api<UserSecureDto>("/users/login", {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function refresh(data: RefreshTokenRequest) {
    return api<UserSecureDto>("/users/refresh", {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function listUsers(offset: number, limit: number) {
    return api<PageResult<UserDto>>(`/users?offset=${offset}&limit=${limit}`, {
        method: "GET"
    })
}

export function deleteUser(id: string) {
    return api<PageResult<UserDto>>(`/users/${id}`, {
        method: "DELETE"
    })
}

export function createUser(data: CreateUserRequest) {
    return api<PageResult<UserDto>>(`/users`, {
        method: "POST",
        body: JSON.stringify(data),
    })
}

export function updateUser(data: UpdateUserRequest) {
    return api<UserDto>(`/users`, {
        method: "PUT",
        body: JSON.stringify(data),
    })
}
