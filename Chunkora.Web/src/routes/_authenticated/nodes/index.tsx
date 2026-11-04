import {createFileRoute, Link} from '@tanstack/react-router'
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {useEffect, useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    Dialog, DialogClose,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table.tsx";
import {
    PiArrowCounterClockwise,
    PiCaretLeft,
    PiCaretRight,
    PiDotsThreeBold,
    PiNotePencil,
    PiPlus,
    PiTrash
} from "react-icons/pi";
import {createNode, deleteNode, listNodes, updateNode} from "@/api/node.ts";
import type {NodeDto} from "@/types/node.ts";

export const Route = createFileRoute('/_authenticated/nodes/')({
  component: NodesPage,
})

function NodesPage() {
    const queryClient = useQueryClient()

    const PAGE_SIZE = 20

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [agentToken, setAgentToken] = useState("");

    const [updatingId, setUpdatingId] = useState("");
    const [updatedName, setUpdatedName] = useState("");
    const [updatedAgentToken, setUpdatedAgentToken] = useState("");
    const [updatedDescription, setUpdatedDescription] = useState("");

    const [creating, setCreating] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [updating, setUpdating] = useState(false);
    const [openCreating, setOpenCreating] = useState(false);
    const [openEditing, setOpenEditing] = useState(false);

    const [page, setPage] = useState(1);

    const nodesQuery = useQuery({
        queryKey: ["nodes", page],
        queryFn: () => listNodes((page - 1) * PAGE_SIZE, PAGE_SIZE),
    })

    const nodes = nodesQuery.data?.items ?? []
    const totalPage = Math.ceil((nodesQuery.data?.total ?? 0) / PAGE_SIZE)

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

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["nodes"],
        })
    }

    const create = async () => {
        if (!name || !agentToken) {
            toast.add({
                type: "error",
                title: "Failed to create node.",
                description: "The name and token of the node should be non-empty",
            })
            return
        }
        setCreating(true)
        try {
            await createNode({
                name: name,
                description: description,
                agentToken: agentToken,
            })
            toast.add({
                type: "success",
                title: `Create node ${name} successfully.`,
            })
            setOpenCreating(false)
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create node.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreating(false)
        }
    }

    const update = async () => {
        setUpdating(true)
        try {
            await updateNode({
                id: updatingId,
                name: updatedName,
                agentToken: updatedAgentToken,
                description: updatedDescription,
            })
            toast.add({
                type: "success",
                title: `Update node ${name} successfully.`,
            })
            setOpenEditing(false)
            setUpdatedName("")
            setUpdatedAgentToken("")
            setUpdatedDescription("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update node.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdating(false)
        }
    }

    const setUpdatingInfo = (node: NodeDto) => {
        setUpdatingId(node.id)
        setUpdatedName(node.name)
        setUpdatedDescription(node.description)
    }

    const del = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to delete node.",
                description: "The id of the node should be non-empty.",
            })
            return
        }
        setDeleting(true)
        try {
            await deleteNode(id)
            toast.add({
                type: "success",
                title: `Delete node ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete node.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setDeleting(false)
        }
    }

    return (
        <div className="m-8">
            <div className="flex flex-row">
                <div className="text-2xl font-bold">Nodes</div>
                <div className="flex-1 text-right">
                    <Button className="w-32 mr-6" variant="outline" onClick={refresh}>
                        <PiArrowCounterClockwise />
                        Refresh
                    </Button>
                    <Dialog open={openCreating} onOpenChange={setOpenCreating}>
                        <DialogTrigger render={
                            <Button className="w-32">
                                <PiPlus />
                                Create
                            </Button>
                        } />
                        <DialogContent>
                            <DialogHeader>
                                <DialogTitle>Create Node</DialogTitle>
                            </DialogHeader>
                            <FieldGroup>
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
                                    <FieldLabel>Agent Token</FieldLabel>
                                    <Input value={agentToken}
                                           onChange={(event) => setAgentToken(event.target.value)}
                                           type="password" />
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
            <div className="mt-2 text-muted-foreground">Manage connected nodes, monitor their status, and configure node settings.</div>
            <Table className="mt-4">
                <TableHeader>
                    <TableRow>
                        <TableHead>Name</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead>Actions</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {
                        nodes.map((node) => (
                            <TableRow key={node.name}>
                                <TableCell>{node.name}</TableCell>
                                <TableCell>{node.description}</TableCell>
                                <TableCell>
                                    <Button className="w-32 mr-6" 
                                            variant="outline" 
                                            render={<Link to="/nodes/$id" params={{ id: node.id }}/>}
                                    >
                                        <PiDotsThreeBold />
                                        Detail
                                    </Button>
                                    <Button className="w-32 mr-6" variant="destructive" onClick={() => del(node.id)} disabled={deleting}>
                                        <PiTrash />
                                        Delete
                                    </Button>
                                    <Dialog open={openEditing} onOpenChange={setOpenEditing}>
                                        <DialogTrigger render={
                                            <Button className="w-32" variant="outline" onClick={() => setUpdatingInfo(node)}>
                                                <PiNotePencil />
                                                Edit
                                            </Button>
                                        } />
                                        <DialogContent>
                                            <DialogHeader>
                                                <DialogTitle>Edit Node</DialogTitle>
                                            </DialogHeader>
                                            <FieldGroup>
                                                <Field>
                                                    <FieldLabel>Name</FieldLabel>
                                                    <Input value={updatedName}
                                                           onChange={(event) => setUpdatedName(event.target.value)}
                                                           type="text" />
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Description</FieldLabel>
                                                    <Input value={updatedDescription}
                                                           onChange={(event) => setUpdatedDescription(event.target.value)}
                                                           type="text" />
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Agent Token</FieldLabel>
                                                    <Input value={updatedAgentToken}
                                                           onChange={(event) => setUpdatedAgentToken(event.target.value)}
                                                           type="password" />
                                                </Field>
                                            </FieldGroup>
                                            <DialogFooter>
                                                <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                                <Button type="submit" onClick={update} disabled={updating}>Create</Button>
                                            </DialogFooter>
                                        </DialogContent>
                                    </Dialog>
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
