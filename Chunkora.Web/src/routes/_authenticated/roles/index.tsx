import {createFileRoute, Link} from '@tanstack/react-router'
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {useEffect, useState} from "react";
import {deleteRole, listRoles} from "@/api/role.ts";
import {toast} from "@/components/ui/toast.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    Dialog, DialogContent,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import {
    PiArrowCounterClockwise,
    PiCaretLeft,
    PiCaretRight,
    PiDotsThreeBold,
    PiNotePencil,
    PiPlus,
    PiTrash
} from "react-icons/pi";

export const Route = createFileRoute('/_authenticated/roles/')({
  component: RolesPage,
})

function RolesPage() {
    const queryClient = useQueryClient()

    const PAGE_SIZE = 20
    
    const [deleting, setDeleting] = useState(false);

    const [page, setPage] = useState(1);

    const rolesQuery = useQuery({
        queryKey: ["roles", page],
        queryFn: () => listRoles((page - 1) * PAGE_SIZE, PAGE_SIZE),
    })

    const roles = rolesQuery.data?.items ?? []
    const totalPage = Math.ceil((rolesQuery.data?.total ?? 0) / PAGE_SIZE)

    useEffect(() => {
        if (!rolesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch roles data.",
            description: rolesQuery.error.message,
        })
    }, [rolesQuery.error]);

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["roles"],
        })
    }

    const del = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to delete role.",
                description: "The id of the role should be non-empty.",
            })
            return
        }
        setDeleting(true)
        try {
            await deleteRole(id)
            toast.add({
                type: "success",
                title: `Delete role ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete role.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setDeleting(false)
        }
    }

    return (
        <div className="m-8">
            <div className="flex flex-row">
                <div className="text-2xl font-bold">Roles</div>
                <div className="text-right flex-1">
                    <Button className="w-32 mr-6" variant="outline" onClick={refresh}>
                        <PiArrowCounterClockwise />
                        Refresh
                    </Button>
                    <Button className="w-32" render={<Link to="/roles/create" />}>
                        <PiPlus />
                        Create
                    </Button>
                </div>
            </div>
            <div className="mt-2 text-muted-foreground">Create and manage roles, defining permissions and access levels across the platform.</div>
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
                        roles.map((role) => (
                            <TableRow key={role.name}>
                                <TableCell>{role.name}</TableCell>
                                <TableCell>{role.description}</TableCell>
                                <TableCell>
                                    <Dialog>
                                        <DialogTrigger render={
                                            <Button variant="outline" className="w-32 mr-6">
                                                <PiDotsThreeBold />
                                                Detail
                                            </Button>
                                        } />
                                        <DialogContent>
                                            <DialogHeader>
                                                <DialogTitle>Role Detail</DialogTitle>
                                            </DialogHeader>
                                            <FieldGroup>
                                                <Field>
                                                    <FieldLabel>Name</FieldLabel>
                                                    <div>{role.name}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>ID</FieldLabel>
                                                    <div>{role.id}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Description</FieldLabel>
                                                    <div>{role.description}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Permissions</FieldLabel>
                                                    {
                                                        role.permissions.map((permission) => (
                                                            <Badge variant="outline">{permission}</Badge>
                                                        ))
                                                    }
                                                </Field>
                                            </FieldGroup>
                                        </DialogContent>
                                    </Dialog>
                                    <Button className="w-32 mr-6" variant="outline" render={<Link to="/roles/update/$id" params={{ id: role.id }} />}>
                                        <PiNotePencil />
                                        Edit
                                    </Button>
                                    <Button className="w-32" variant="destructive" onClick={() => del(role.id)} disabled={deleting}>
                                        <PiTrash />
                                        Delete
                                    </Button>
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
