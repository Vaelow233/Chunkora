import {
    createFileRoute, Link,
    Outlet,
    redirect, useRouter,
} from "@tanstack/react-router"
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarGroup,
    SidebarHeader,
    SidebarMenu, SidebarMenuButton, SidebarMenuItem,
    SidebarProvider
} from "@/components/ui/sidebar.tsx";

import { useUserStore } from "@/stores/auth"
import {toast} from "@/components/ui/toast.tsx";
import {refresh} from "@/api/user.ts";
import {getSelfRole} from "@/api/role.ts";
import {Badge} from "@/components/ui/badge.tsx";
import {useQueryClient} from "@tanstack/react-query";
import {PiDatabase, PiHouse, PiShieldCheck, PiSignOut, PiStack, PiUserCircle, PiUsers} from "react-icons/pi";

export const Route = createFileRoute("/_authenticated")({
    beforeLoad: async ({ location }) => {
        // TODO: remove the cached query result
        const store = useUserStore.getState();
        if (!store.username || !store.accessToken || !store.refreshToken || !store.accessTokenExpiresAt || 
            !store.refreshTokenExpiresAt || new Date(store.refreshTokenExpiresAt!) <= new Date()) {
            store.logout()
            throw redirect({
                to: "/login",
                search: {
                    redirect: location.href,
                },
            })
        }
        if (new Date(store.accessTokenExpiresAt!) <= new Date()) {
            try {
                const user = await refresh({name: store.username!, refreshToken: store.refreshToken!})
                store.login(user)
            } catch (_) {
                store.logout()
                throw redirect({
                    to: "/login",
                    search: {
                        redirect: location.href,
                    },
                })
            }
        }
        if (!store.role || !store.roleName) {
            try {
                const role = await getSelfRole()
                store.updateRole(role)
            } catch (_) {
                store.logout()
                throw redirect({
                    to: "/login",
                    search: {
                        redirect: location.href,
                    },
                })
            }
        }
    },

    component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
    const store = useUserStore()
    const router = useRouter()
    const queryClient = useQueryClient()

    async function logout() {
        await queryClient.cancelQueries();
        queryClient.clear();
        store.logout()
        await router.navigate({
            to: "/login",
            search: { redirect: "/" },
            replace: true,
        })
        toast.add({
            type: "success",
            title: "Logout successfully.",
        })
    }

    return (
        <SidebarProvider>
            <Sidebar>
                <SidebarHeader>
                    <SidebarMenu>
                        <SidebarMenuItem>
                            <div className="flex items-center justify-center flex-row mt-2">
                                <span className="text-2xl font-bold mr-2">Chunkora</span>
                                <Badge variant="outline">v1.0-alpha</Badge>
                            </div>
                        </SidebarMenuItem>
                    </SidebarMenu>
                </SidebarHeader>
                <SidebarContent>
                    <SidebarGroup>
                        <SidebarMenu>
                            <SidebarMenuItem key="home">
                                <SidebarMenuButton render={<Link to="/" />}>
                                    <div className="flex items-center">
                                        <PiHouse />
                                        <span className="ms-2">Home</span>
                                    </div>
                                </SidebarMenuButton>
                            </SidebarMenuItem>
                            {
                                !store.canManageUsers() ? <></> : (
                                    <SidebarMenuItem key="users">
                                        <SidebarMenuButton render={<Link to="/users" />}>
                                            <div className="flex items-center">
                                                <PiUsers />
                                                <span className="ms-2">Users</span>
                                            </div>
                                        </SidebarMenuButton>
                                    </SidebarMenuItem>
                                )
                            }
                            {
                                !store.canManageRoles() ? <></> : (
                                    <SidebarMenuItem key="roles">
                                        <SidebarMenuButton render={<Link to="/roles" />}>
                                            <div className="flex items-center">
                                                <PiShieldCheck />
                                                <span className="ms-2">Roles</span>
                                            </div>
                                        </SidebarMenuButton>
                                    </SidebarMenuItem>
                                )
                            }
                            {
                                !store.canManageNodes() ? <></> : (
                                    <SidebarMenuItem key="nodes">
                                        <SidebarMenuButton render={<Link to="/nodes" />}>
                                            <div className="flex items-center">
                                                <PiDatabase />
                                                <span className="ms-2">Nodes</span>
                                            </div>
                                        </SidebarMenuButton>
                                    </SidebarMenuItem>
                                )
                            }
                            <SidebarMenuItem key="instances">
                                <SidebarMenuButton render={<Link to="/instances" />}>
                                    <div className="flex items-center">
                                        <PiStack />
                                        <span className="ms-2">Instances</span>
                                    </div>
                                </SidebarMenuButton>
                            </SidebarMenuItem>
                        </SidebarMenu>
                    </SidebarGroup>
                </SidebarContent>
                <SidebarFooter>
                    <SidebarMenu>
                        <SidebarMenuItem>
                            <SidebarMenuButton>
                                <div className="flex items-center">
                                    <PiUserCircle />
                                    <span className="ms-2">{store.username}</span>
                                    <Badge className="ml-2 bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300">
                                        {store.roleName}
                                    </Badge>
                                </div>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                        <SidebarMenuItem>
                            <SidebarMenuButton onClick={logout}>
                                <div className="flex items-center">
                                    <PiSignOut />
                                    <span className="ms-2">Logout</span>
                                </div>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                    </SidebarMenu>
                </SidebarFooter>
            </Sidebar>
            <main className="w-full flex-1">
                <Outlet />
            </main>
        </SidebarProvider>
    )
}
