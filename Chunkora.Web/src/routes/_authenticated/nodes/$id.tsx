import {createFileRoute, Link} from "@tanstack/react-router";
import {useQuery} from "@tanstack/react-query";
import {getNode, getNodeMetrics, getNodeSecure, getNodeStatus} from "@/api/node.ts";
import {useEffect, useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {InfoField} from "@/components/InfoField.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import {Line, LineChart, XAxis} from 'recharts';
import type {NodeMetricsDto} from "@/types/node.ts";
import {type ChartConfig, ChartContainer, ChartTooltip, ChartTooltipContent} from "@/components/ui/chart.tsx";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbPage,
    BreadcrumbSeparator
} from "@/components/ui/breadcrumb";
import {Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {
    PiAddressBook,
    PiArchive,
    PiCpu,
    PiLaptop,
    PiMemory,
    PiShield,
    PiSpeedometer,
    PiTagChevron,
    PiTerminal
} from "react-icons/pi";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";

export const Route = createFileRoute('/_authenticated/nodes/$id')({
    component: NodeDetailPage,
})

const chartConfig = {
    cpuUsage: {
        label: "CPU Usage",
        color: "var(--chart-cpu)",
    },
    memoryUsedGigabytes: {
        label: "Memory Usage",
        color: "var(--chart-memory)",
    }
} satisfies ChartConfig

function handleMetricsData(data: NodeMetricsDto[]) {
    return data.map(item => ({
        ...item,
        memoryUsedGigabytes: item.memoryUsedBytes / Math.pow(10, 9),
        timestamp: new Date(item.createdAt).getTime(),
    }))
}

function getLatestMetrics(data: NodeMetricsDto[]) {
    if (data.length === 0) return null;
    if (data.length === 1) return data[0];
    return data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
}

function NodeDetailPage() {
    const { id } = Route.useParams()
    
    const [open, setOpen] = useState(false)
    
    const nodeSecureQuery = useQuery({
        queryKey: ["node", "secure", id],
        queryFn: () => getNodeSecure(id),
    })

    const nodeQuery = useQuery({
        queryKey: ["node", id],
        queryFn: () => getNode(id),
    })
    
    const nodeStatusQuery = useQuery({
        queryKey: ["node", "status", id],
        queryFn: () => getNodeStatus(id),
    })
    
    const nodeMetricsQuery = useQuery({
        queryKey: ["node", "metrics", id],
        queryFn: () => getNodeMetrics(
            id,
            new Date(Date.now() - 60 * 60 * 1000).toISOString(),
            new Date().toISOString(),
            0,
            2147483647
        ),
        refetchInterval: 10000,
    })

    useEffect(() => {
        if (!nodeQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to fetch the data of node ${id}.`,
            description: nodeQuery.error.message,
        })
    }, [nodeQuery.error])

    const latestMetrics = nodeMetricsQuery.data ? getLatestMetrics(nodeMetricsQuery.data.items) : null
    const handledMetrics = nodeMetricsQuery.data ? handleMetricsData(nodeMetricsQuery.data.items) : null
    
    return (
        <div className="m-8">
            <Breadcrumb>
                <BreadcrumbList>
                    <BreadcrumbItem>
                        <BreadcrumbLink render={<Link to="/nodes"/>}>Nodes</BreadcrumbLink>
                    </BreadcrumbItem>
                    <BreadcrumbSeparator />
                    <BreadcrumbItem>
                        <BreadcrumbPage>{nodeQuery.data?.name ?? id}</BreadcrumbPage>
                    </BreadcrumbItem>
                </BreadcrumbList>
            </Breadcrumb>
            <div className="flex flex-row mt-4 gap-4 items-center">
                <div className="w-16 h-16 bg-primary-foreground flex justify-center items-center rounded-lg">
                    <PiLaptop className="w-12 h-12 text-primary" />
                </div>
                <div className="flex flex-col">
                    <div className="flex flex-row items-center gap-2">
                        <div className="text-2xl font-bold">{nodeQuery.data?.name}</div>
                        {
                            nodeStatusQuery.data?.instanceConnected && nodeStatusQuery.data?.instanceOperationConnected ? 
                                <Badge className="bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-300">Online</Badge> :
                                <Badge className="bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300">Disconnected</Badge>
                        }
                        <div className="text-muted-foreground text-sm">Description: {nodeQuery.data?.description}</div>
                    </div>
                    <div>ID: {nodeQuery.data?.id}</div>
                </div>
                {
                    !nodeSecureQuery.data ? <></> : (
                        <div className="ml-auto shrink-0">
                            <Dialog open={open} onOpenChange={setOpen}>
                                <DialogTrigger render={
                                    <Button variant="outline" className="w-48">
                                        <PiShield />
                                        Secret Information
                                    </Button>
                                } />
                                <DialogContent>
                                    <DialogHeader>
                                        <DialogTitle>Secret Information</DialogTitle>
                                    </DialogHeader>
                                    <FieldGroup>
                                        <Field>
                                            <FieldLabel>Agent Token</FieldLabel>
                                            <div>{nodeSecureQuery.data.agentToken}</div>
                                        </Field>
                                    </FieldGroup>
                                </DialogContent>
                            </Dialog>
                        </div>
                    )
                }
            </div>
            <div className="flex flex-row gap-6 mt-4">
                {
                    (!nodeMetricsQuery.data || !latestMetrics) ? <></> : (
                        <>
                            <InfoField label="Architecture" icon={<PiCpu className="text-primary" />}>
                                {latestMetrics!.architecture}
                            </InfoField>
                            <InfoField label="Hostname" icon={<PiAddressBook className="text-secondary" />}>
                                {latestMetrics!.hostname}
                            </InfoField>
                            <InfoField label="OS" icon={<PiArchive className="text-tertiary" />}>
                                {latestMetrics!.osType}
                            </InfoField>
                            <InfoField label="OS Detail" icon={<PiTerminal />}>
                                {latestMetrics!.osDetail}
                            </InfoField>
                            <InfoField label="Agent Version" icon={<PiTagChevron />}>
                                {latestMetrics!.version}
                            </InfoField>
                        </>
                    )
                }
            </div>
            {
                (!nodeMetricsQuery.data || !latestMetrics) ? <></> : (
                    <div className="flex flex-row gap-6 mt-6">
                        <Card className="w-full">
                            <CardHeader>
                                <CardTitle>
                                    <div className="text-muted-foreground">CPU Usage</div>
                                </CardTitle>
                                <CardDescription>
                                    <div className="font-bold text-4xl text-foreground">{latestMetrics!.cpuUsage} %</div>
                                </CardDescription>
                                <CardAction>
                                    <div className="w-16 h-16 bg-primary-foreground flex justify-center items-center rounded-lg">
                                        <PiSpeedometer className="w-12 h-12 text-primary" />
                                    </div>
                                </CardAction>
                            </CardHeader>
                            <CardContent>
                                <ChartContainer config={chartConfig} className="w-full aspect-4/1">
                                    <LineChart data={handledMetrics!}>
                                        <Line type="monotone" dataKey="cpuUsage" stroke="var(--primary)" strokeWidth={2} dot={false} />
                                        <XAxis
                                            dataKey="timestamp"
                                            type="number"
                                            scale="time"
                                            domain={["dataMin", "dataMax"]}
                                            tickFormatter={(value: number) =>
                                                new Date(value).toLocaleTimeString("en-GB", {
                                                    hour: "2-digit",
                                                    minute: "2-digit",
                                                    second: "2-digit",
                                                })
                                            }
                                        />
                                        <ChartTooltip content={<ChartTooltipContent />} />
                                    </LineChart>
                                </ChartContainer>
                            </CardContent>
                        </Card>
                        <Card className="w-full">
                            <CardHeader>
                                <CardTitle>
                                    <div className="text-muted-foreground">Memory Usage</div>
                                </CardTitle>
                                <CardDescription>
                                    <div className="font-bold text-4xl text-foreground">
                                        {(latestMetrics!.memoryUsedBytes / Math.pow(1024, 3)).toFixed(2)} GB
                                    </div>
                                </CardDescription>
                                <CardAction>
                                    <div className="w-16 h-16 bg-secondary-foreground flex justify-center items-center rounded-lg">
                                        <PiMemory className="w-12 h-12 text-secondary" />
                                    </div>
                                </CardAction>
                            </CardHeader>
                            <CardContent>
                                <ChartContainer config={chartConfig} className="w-full aspect-4/1">
                                    <LineChart data={handledMetrics!}>
                                        <Line type="monotone" dataKey="memoryUsedGigabytes" stroke="var(--secondary)" strokeWidth={2} dot={false} />
                                        <XAxis
                                            dataKey="timestamp"
                                            type="number"
                                            scale="time"
                                            domain={["dataMin", "dataMax"]}
                                            tickFormatter={(value: number) =>
                                                new Date(value).toLocaleTimeString("en-GB", {
                                                    hour: "2-digit",
                                                    minute: "2-digit",
                                                    second: "2-digit",
                                                })
                                            }
                                        />
                                        <ChartTooltip content={<ChartTooltipContent />} />
                                    </LineChart>
                                </ChartContainer>
                            </CardContent>
                        </Card>
                    </div>
                )
            }
        </div>
    )
}