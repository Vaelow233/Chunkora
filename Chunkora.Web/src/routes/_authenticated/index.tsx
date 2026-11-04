import {createFileRoute} from "@tanstack/react-router"
import {Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {useUserStore} from "@/stores/auth.ts";
import { useQuery } from "@tanstack/react-query";
import {toast} from "@/components/ui/toast.tsx";
import {useEffect} from "react";
import {listUsers} from "@/api/user.ts";
import {listNodes} from "@/api/node.ts";
import {listInstances} from "@/api/instance.ts";
import {PiDatabase, PiStack, PiUsers} from "react-icons/pi";

export const Route = createFileRoute("/_authenticated/")({
    component: HomePage,
})

function HomePage() {
    const store = useUserStore();
    
    const usersQuery = useQuery({
        enabled: store.canManageUsers(),
        queryKey: ["users", "count"],
        queryFn: () => listUsers(0, 0),
    })
    
    const nodesQuery = useQuery({
        enabled: store.canManageNodes(),
        queryKey: ["nodes", "count"],
        queryFn: () => listNodes(0, 0),
    })
    
    const instancesQuery = useQuery({
        queryKey: ["instances", "count"],
        queryFn: () => listInstances(0, 0),
    })

    useEffect(() => {
        if (!usersQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch users data.",
            description: usersQuery.error.message,
        })
    }, [usersQuery.error])

    useEffect(() => {
        if (!nodesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch nodes data.",
            description: nodesQuery.error.message,
        })
    }, [nodesQuery.error])

    useEffect(() => {
        if (!instancesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch instances data.",
            description: instancesQuery.error.message,
        })
    }, [instancesQuery.error])

    return (
        <div className="m-8">
            <div className="text-2xl font-bold">Overview</div>
            <div className="flex flex-row flex-4 justify-center gap-12 mt-6">
                {
                    !store.canManageUsers() ? <></> : (
                        <Card className="flex-1">
                            <CardHeader>
                                <CardTitle>Users</CardTitle>
                                <CardDescription>Total number of registered users on the platform</CardDescription>
                                <CardAction>
                                    <div className="w-10 h-10 bg-primary-foreground flex justify-center items-center rounded-lg">
                                        <PiUsers className="w-6 h-6 text-primary" />
                                    </div>
                                </CardAction>
                            </CardHeader>
                            <CardContent>
                                <div className="font-bold text-3xl">{usersQuery.data?.total ?? 'N/A'}</div>
                            </CardContent>
                        </Card>
                    )
                }
                {
                    !store.canManageNodes() ? <></> : (
                        <Card className="flex-1">
                            <CardHeader>
                                <CardTitle>Nodes</CardTitle>
                                <CardDescription>Total number of nodes currently registered in the system</CardDescription>
                                <CardAction>
                                    <div className="w-10 h-10 bg-secondary-foreground flex justify-center items-center rounded-lg">
                                        <PiDatabase className="w-6 h-6 text-secondary" />
                                    </div>
                                </CardAction>
                            </CardHeader>
                            <CardContent>
                                <div className="font-bold text-3xl">{nodesQuery.data?.total ?? 'N/A'}</div>
                            </CardContent>
                        </Card>
                    )
                }
                <Card className="flex-1">
                    <CardHeader>
                        <CardTitle>Instances</CardTitle>
                        <CardDescription>Total number of instances managed across all nodes</CardDescription>
                        <CardAction>
                            <div className="w-10 h-10 bg-tertiary-foreground flex justify-center items-center rounded-lg">
                                <PiStack className="w-6 h-6 text-tertiary" />
                            </div>
                        </CardAction>
                    </CardHeader>
                    <CardContent>
                        <div className="font-bold text-3xl">{instancesQuery.data?.total ?? 'N/A'}</div>
                    </CardContent>
                </Card>
            </div>
        </div>
    )
}