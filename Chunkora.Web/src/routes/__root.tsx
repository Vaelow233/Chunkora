import {
    createRootRoute,
    Outlet,
} from "@tanstack/react-router"
import {Toaster} from "@/components/ui/toast.tsx";

export const Route = createRootRoute({
    component: RootComponent,
})

function RootComponent() {
    return (
        <>
            <Outlet />
            <Toaster />
        </>
    )
}