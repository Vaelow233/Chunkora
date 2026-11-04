import { create } from "zustand"
import { persist } from "zustand/middleware"
import type {UserSecureDto} from "@/types/user.ts";
import type {RoleDto} from "@/types/role.ts";

interface UserStore {
    username: string | null,
    accessToken: string | null,
    accessTokenExpiresAt: string | null,
    refreshToken: string | null,
    refreshTokenExpiresAt: string | null,
    role: string | null,
    roleName: string | null,
    permissions: string[]

    login: (user: UserSecureDto) => void
    updateRole: (role: RoleDto) => void
    logout: () => void
    canManageUsers: () => boolean
    canManageRoles: () => boolean
    canManageNodes: () => boolean
    hasPermission: (permission: string) => boolean
    hasPermissionPrefix: (prefix: string) => boolean
    tryMatch: (ownedPermission: string, required: string[], captureIndex: number) => {captured: string | null, matched: boolean};
}

export const useUserStore = create<UserStore>()(
    persist(
        (set, get) => ({
            username: null,
            accessToken: null,
            accessTokenExpiresAt: null,
            refreshToken: null,
            refreshTokenExpiresAt: null,
            role: null,
            roleName: null,
            permissions: [],

            login: (user: UserSecureDto) => {
                set({
                    username: user.name,
                    accessToken: user.accessToken, 
                    accessTokenExpiresAt: user.accessTokenExpiresAt,
                    refreshToken: user.refreshToken,
                    refreshTokenExpiresAt: user.refreshTokenExpiresAt,
                })
            },
            
            updateRole: (role: RoleDto) => {
                set({
                    role: role.id,
                    roleName: role.name,
                    permissions: role.permissions,
                })
            },

            logout: () => {
                set({
                    username: null, 
                    accessToken: null, 
                    accessTokenExpiresAt: null, 
                    refreshToken: null, 
                    refreshTokenExpiresAt: null,
                    role: null,
                    roleName: null,
                    permissions: [],
                })
            },
            
            hasPermission: (required: string) => {
                const { permissions } = get()
                const parts = required.split('.');
                return permissions.some(owned => get().tryMatch(owned, parts, -1).matched);
            },

            hasPermissionPrefix: (prefix: string) => {
                const { permissions } = get()
                return permissions.some(owned => hasPermissionPrefix(owned, prefix))

                function hasPermissionPrefix(ownedPermission: string, requiredPrefix: string) {
                    const owned = ownedPermission.split(".")
                    const required = requiredPrefix.split(".")
                    for (let i = 0; i < required.length; i++) {
                        if (i >= owned.length) {
                            return owned[owned.length - 1] === "*"
                        }
                        if (owned[i] === "*") {
                            if (i === owned.length - 1) {
                                return true
                            }
                            continue
                        }
                        if (owned[i] !== required[i]) {
                            return false
                        }
                    }
                    return true
                }
            },
            
            tryMatch: (ownedPermission: string, required: string[], captureIndex: number) => {
                let captured: string | null = null;
                const owned = ownedPermission.split('.');
                for (let i = 0; i < owned.length; i++) {
                    if (i >= required.length) return {captured: null, matched: false}

                    if (owned[i] == "*") {
                        if (i == owned.length - 1) {
                            if (captureIndex >= i) captured = "*";
                            return {captured: captured, matched: true}
                        }
                        if (i == captureIndex) captured = "*";
                        continue;
                    }

                    if (i == captureIndex) {
                        captured = owned[i];
                        continue;
                    }

                    if (owned[i] != required[i]) {
                        return {captured: null, matched: false}
                    }
                }

                const matched = owned.length === required.length;
                return {
                    captured: matched ? captured : null,
                    matched,
                }
            },

            canManageUsers: () => {
                return get().hasPermissionPrefix("admin.user")
            },

            canManageRoles: () => {
                return get().hasPermissionPrefix("admin.role")
            },
            
            canManageNodes: () => {
                return get().hasPermissionPrefix("admin.node")
            },
        }),
        {
            name: "chunkora-web-auth",
        }
    ),
)