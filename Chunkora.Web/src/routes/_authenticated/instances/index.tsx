import {createInstance, deleteInstance, killInstance, listInstances, startInstance, stopInstance} from "@/api/instance";
import { listNodes } from "@/api/node";
import { Button } from "@/components/ui/button";
import { Dialog, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { toast } from "@/components/ui/toast";
import {useQuery, useQueryClient} from "@tanstack/react-query";
import { createFileRoute, Link } from '@tanstack/react-router'
import {useEffect, useState} from "react";
import {
    PiArrowCounterClockwise,
    PiCaretLeft,
    PiCaretRight,
    PiDotsThreeBold, PiPause,
    PiPlay,
    PiPlus, PiSkull,
    PiTrash
} from "react-icons/pi";
import {
    Select,
    SelectContent,
    SelectGroup,
    SelectItem,
    SelectLabel,
    SelectTrigger,
    SelectValue
} from "@/components/ui/select.tsx";
import {InstanceStatusBadge} from "@/components/instances/InstanceStatusBadge.tsx";

export const Route = createFileRoute('/_authenticated/instances/')({
  component: InstancesPage,
})

function InstancesPage() {
    const queryClient = useQueryClient()

    const PAGE_SIZE = 20

    const [node, setNode] = useState("");
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [path, setPath] = useState("");
    const [launchCommand, setLaunchCommand] = useState("");
    const [stopCommand, setStopCommand] = useState("");

    const [creating, setCreating] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [open, setOpen] = useState(false);
    
    // instance status related
    const [starting, setStarting] = useState(false);
    const [stopping, setStopping] = useState(false);
    const [killing, setKilling] = useState(false);

    const [page, setPage] = useState(1);

    const nodesQuery = useQuery({
        queryKey: ["nodes"],
        queryFn: () => listNodes(0, 2147483647),
    })

    const instancesQuery = useQuery({
        queryKey: ["instances", page],
        queryFn: () => listInstances((page - 1) * PAGE_SIZE, PAGE_SIZE),
    })

    const instances = instancesQuery.data?.items ?? []
    const totalPage = Math.ceil((instancesQuery.data?.total ?? 0) / PAGE_SIZE)

    useEffect(() => {
        if (!nodesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch nodes data. You may not create a new instance because there are no nodes to choose.",
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

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["instances"],
        })
    }

    const create = async () => {
        if (!node || !name || !path) {
            toast.add({
                type: "error",
                title: "Failed to create instance.",
                description: "The node, name and path of the instance should be non-empty.",
            })
            return
        }
        setCreating(true)
        try {
            await createInstance({
                name: name,
                description: description,
                nodeId: node,
                path: path,
                launchCommand: launchCommand,
                stopCommand: stopCommand,
            })
            toast.add({
                type: "success",
                title: `Create instance ${name} successfully.`,
            })
            setOpen(false)
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreating(false)
        }
    }

    const del = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to delete instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setDeleting(true)
        try {
            await deleteInstance(id)
            toast.add({
                type: "success",
                title: `Delete instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setDeleting(false)
        }
    }

    const start = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to start instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setStarting(true)
        try {
            await startInstance(id)
            toast.add({
                type: "success",
                title: `Start instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to start instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setStarting(false)
        }
    }

    const stop = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to stop instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setStopping(true)
        try {
            await stopInstance(id)
            toast.add({
                type: "success",
                title: `Stop instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to stop instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setStopping(false)
        }
    }

    const kill = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to kill instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setKilling(true)
        try {
            await killInstance(id)
            toast.add({
                type: "success",
                title: `Kill instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to kill instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setKilling(false)
        }
    }

    return (
        <div className="m-8">
            <div className="flex flex-row">
                <div className="text-2xl font-bold">Instances</div>
                <div className="flex-1 text-right">
                    <Button className="w-32 mr-6" onClick={refresh}>
                        <PiArrowCounterClockwise />
                        Refresh
                    </Button>
                    <Dialog open={open} onOpenChange={setOpen}>
                        <DialogTrigger render={
                            <Button className="w-32">
                                <PiPlus />
                               Create
                            </Button>
                        } />
                        <DialogContent>
                            <DialogHeader>
                                <DialogTitle>Create Instance</DialogTitle>
                            </DialogHeader>
                            <FieldGroup>
                                <Field>
                                    <FieldLabel>Node</FieldLabel>
                                    <Select value={node}
                                            onValueChange={(newValue) => setNode(newValue ?? "")}
                                            items={(nodesQuery.data?.items ?? []).map(node => ({value: node.id, label: node.name}))}>
                                        <SelectTrigger className="w-full max-w-48">
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectGroup>
                                                <SelectLabel>Nodes</SelectLabel>
                                                {
                                                    nodesQuery.data?.items.map((node) => (
                                                        <SelectItem key={node.id} value={node.id}>
                                                            {node.name}
                                                        </SelectItem>
                                                    ))
                                                }
                                            </SelectGroup>
                                        </SelectContent>
                                    </Select>
                                </Field>
                                <Field>
                                    <FieldLabel>Name</FieldLabel>
                                    <Input value={name}
                                           onChange={(event) => setName(event.target.value)}
                                           type="text" />
                                </Field>
                                <Field>
                                    <FieldLabel>Description</FieldLabel>
                                    <Input value={description}
                                           onChange={(event) => setDescription(event.target.value)}
                                           type="text" />
                                </Field>
                                <Field>
                                    <FieldLabel>Path</FieldLabel>
                                    <Input value={path}
                                           onChange={(event) => setPath(event.target.value)}
                                           type="text" />
                                </Field>
                                <Field>
                                    <FieldLabel>Launch Command</FieldLabel>
                                    <Input value={launchCommand}
                                           onChange={(event) => setLaunchCommand(event.target.value)}
                                           type="text" />
                                </Field>
                                <Field>
                                    <FieldLabel>Stop Command</FieldLabel>
                                    <Input value={stopCommand}
                                           onChange={(event) => setStopCommand(event.target.value)}
                                           type="text" />
                                </Field>
                            </FieldGroup>
                            <DialogFooter>
                                <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                <Button type="submit" onClick={create} disabled={creating}>Create</Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>
                </div>
            </div>
            <div className="mt-2 text-muted-foreground">View and manage instances across your nodes, including their status and resource usage.</div>
            <Table className="mt-4">
                <TableHeader>
                    <TableRow>
                        <TableHead>Name</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead>Actions</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {
                        instances.map((instance) => (
                            <TableRow key={instance.name}>
                                <TableCell>{instance.name}</TableCell>
                                <TableCell>
                                    <InstanceStatusBadge status={instance.status} />
                                </TableCell>
                                <TableCell>{instance.description}</TableCell>
                                <TableCell>
                                    <Button className="w-32"
                                            variant="outline"
                                            render={<Link to="/instances/$id" params={{ id: instance.id }}/>}
                                    >
                                        <PiDotsThreeBold/>
                                        Detail
                                    </Button>
                                    <Button className="ml-6 w-32" variant="destructive" onClick={() => del(instance.id)} disabled={deleting}>
                                        <PiTrash />
                                        Delete
                                    </Button>
                                    {
                                        instance.status === 0 ? (
                                            <Button 
                                                className="ml-6 w-32" 
                                                variant="outline" 
                                                onClick={() => start(instance.id)} disabled={starting}
                                            >
                                                <PiPlay />
                                                Start
                                            </Button>
                                        ) : <></>
                                    }
                                    {
                                        instance.status === 2 ? (
                                            <Button
                                                className="ml-6 w-32"
                                                variant="outline"
                                                onClick={() => stop(instance.id)} disabled={stopping}
                                            >
                                                <PiPause />
                                                Stop
                                            </Button>
                                        ) : <></>
                                    }
                                    {
                                        instance.status !== 0 ? (
                                            <Button
                                                className="ml-6 w-32"
                                                variant="destructive"
                                                onClick={() => kill(instance.id)} disabled={killing}
                                            >
                                                <PiSkull />
                                                Kill
                                            </Button>
                                        ) : <></>
                                    }
                                </TableCell>
                            </TableRow>
                        ))
                    }
                </TableBody>
            </Table>
            <div className="flex flex-row mt-6 gap-3 float-right items-center">
                <Button variant="outline" size="icon-sm" onClick={() => setPage(page - 1)} disabled={page <= 1}>
                    <PiCaretLeft />
                </Button>
                <div>{page} / {totalPage}</div>
                <Button variant="outline" size="icon-sm" onClick={() => setPage(page + 1)} disabled={page >= totalPage}>
                    <PiCaretRight />
                </Button>
            </div>
        </div>
    )
}
