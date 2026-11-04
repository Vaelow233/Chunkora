import { StrictMode } from "react"
import ReactDOM from "react-dom/client"
import {createRouter, RouterProvider} from "@tanstack/react-router"

import { routeTree } from "./routeTree.gen"

import "./index.css"
import "./globals.css"
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {ThemeProvider} from "@/components/theme-provider.tsx";

const queryClient = new QueryClient()

const router = createRouter({
    routeTree,
})

declare module "@tanstack/react-router" {
    interface Register {
        router: typeof router
    }
}

const rootElement = document.getElementById("root")!

if (!rootElement.innerHTML) {
    ReactDOM.createRoot(rootElement).render(
        <StrictMode>
            <ThemeProvider>
                <QueryClientProvider client={queryClient}>
                    <RouterProvider router={router} />
                </QueryClientProvider>
            </ThemeProvider>
        </StrictMode>,
    )
}