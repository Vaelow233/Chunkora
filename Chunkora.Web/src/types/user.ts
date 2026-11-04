export interface UserDto {
    id: string
    roleId: string
    name: string
    description: string
    lastLoginAt: string | null
}

export interface UserSecureDto {
    id: string
    roleId: string
    name: string
    description: string
    accessToken: string
    accessTokenExpiresAt: string
    refreshToken: string
    refreshTokenExpiresAt: string
    lastLoginAt: string | null
}

export interface LoginRequest {
    name: string
    password: string
}

export interface RefreshTokenRequest {
    name: string
    refreshToken: string
}

export interface CreateUserRequest {
    name: string
    password: string
    roleId: string
    description: string
}

export interface UpdateUserRequest extends CreateUserRequest {
    id: string
}