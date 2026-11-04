import { createFileRoute } from '@tanstack/react-router'
import {Button} from "@/components/ui/button.tsx";
import {
    Dialog,
    DialogTrigger,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter, DialogClose
} from "@/components/ui/dialog.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {useEffect, useState} from "react";
import {Table, TableHeader, TableRow, TableHead, TableBody, TableCell} from "@/components/ui/table.tsx";
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {createUser, deleteUser, listUsers, updateUser} from "@/api/user.ts";
import {toast} from "@/components/ui/toast.tsx";
import {listRoles} from "@/api/role.ts";
import {
    Select,
    SelectContent,
    SelectGroup,
    SelectItem,
    SelectLabel,
    SelectTrigger,
    SelectValue
} from "@/components/ui/select.tsx";
import {
    PiArrowCounterClockwise,
    PiCaretLeft,
    PiCaretRight,
    PiDotsThreeBold, PiNotePencil,
    PiTrash,
    PiUserPlus
} from "react-icons/pi";
import type {UserDto} from "@/types/user.ts";
export const Route = createFileRoute("/_authenticated/users")({
    component: UsersPage,
})

function UsersPage() {
    const queryClient = useQueryClient()
    
    const PAGE_SIZE = 20
    
    const [name, setName] = useState("");
    const [role, setRole] = useState("");
    const [password, setPassword] = useState("");
    const [description, setDescription] = useState("");

    const [updatingId, setUpdatingId] = useState("");
    const [updatedName, setUpdatedName] = useState("");
    const [updatedRole, setUpdatedRole] = useState("");
    const [updatedPassword, setUpdatedPassword] = useState("");
    const [updatedDescription, setUpdatedDescription] = useState("");
    
    const [creating, setCreating] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [updating, setUpdating] = useState(false);
    const [openCreating, setOpenCreating] = useState(false);
    const [openEditing, setOpenEditing] = useState(false);

    const [page, setPage] = useState(1);

    const usersQuery = useQuery({
        queryKey: ["users", page],
        queryFn: () => listUsers((page - 1) * PAGE_SIZE, PAGE_SIZE),
    })
    
    const rolesQuery = useQuery({
        queryKey: ["roles"],
        queryFn: () => listRoles(0, 2147483647),
    })

    const users = usersQuery.data?.items ?? []
    const totalPage = Math.ceil((usersQuery.data?.total ?? 0) / PAGE_SIZE)

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
        if (!rolesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch roles data. You may not create a new user because there are no roles to choose.",
            description: rolesQuery.error.message,
        })
    }, [rolesQuery.error]);
    
    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["users"],
        })
    }
    
    const create = async () => {
        if (!name || !role || !password) {
            toast.add({
                type: "error",
                title: "Failed to create user.",
                description: "The name, role and password of the user should be non-empty",
            })
            return
        }
        setCreating(true)
        try {
            await createUser({
                name: name,
                roleId: role,
                password: password,
                description: description,
            })
            toast.add({
                type: "success",
                title: `Create user ${name} successfully.`,
            })
            setOpenCreating(false)
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create user.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreating(false)
        }
    }

    const update = async () => {
        setUpdating(true)
        try {
            await updateUser({
                id: updatingId,
                name: updatedName,
                roleId: updatedRole,
                password: updatedPassword,
                description: updatedDescription,
            })
            toast.add({
                type: "success",
                title: `Update user ${name} successfully.`,
            })
            setOpenEditing(false)
            setUpdatedName("")
            setUpdatedRole("")
            setUpdatedPassword("")
            setUpdatedDescription("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update user.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdating(false)
        }
    }
    
    const setUpdatingInfo = (user: UserDto) => {
        setUpdatingId(user.id)
        setUpdatedName(user.name)
        setUpdatedRole(user.roleId)
        setUpdatedDescription(user.description)
    }
    
    const del = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to delete user.",
                description: "The id of the user should be non-empty.",
            })
            return
        }
        setDeleting(true)
        try {
            await deleteUser(id)
            toast.add({
                type: "success",
                title: `Delete user ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete user.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setDeleting(false)
        }
    }
    
    return (
        <div className="m-8">
            <div className="flex flex-row">
                <div className="text-2xl font-bold">Users</div>
                <div className="text-right flex-1">
                    <Button className="w-32 mr-6" variant="outline" onClick={refresh}>
                        <PiArrowCounterClockwise />
                        Refresh
                    </Button>
                    <Dialog open={openCreating} onOpenChange={setOpenCreating}>
                        <DialogTrigger render={
                            <Button className="w-32">
                                <PiUserPlus />
                                Create
                            </Button>
                        } />
                        <DialogContent>
                            <DialogHeader>
                                <DialogTitle>Create User</DialogTitle>
                            </DialogHeader>
                            <FieldGroup>
                                <Field>
                                    <FieldLabel>Name</FieldLabel>
                                    <Input value={name}
                                           onChange={(event) => setName(event.target.value)}
                                           type="text" />
                                </Field>
                                <Field>
                                    <FieldLabel>Role</FieldLabel>
                                    <Select value={role}
                                            onValueChange={(newValue) => setRole(newValue ?? "")}
                                            items={(rolesQuery.data?.items ?? []).map(role => ({value: role.id, label: role.name}))}>
                                        <SelectTrigger className="w-full max-w-48">
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectGroup>
                                                <SelectLabel>Roles</SelectLabel>
                                                {
                                                    rolesQuery.data?.items.map((role) => (
                                                        <SelectItem key={role.id} value={role.id}>
                                                            {role.name}
                                                        </SelectItem>
                                                    ))
                                                }
                                            </SelectGroup>
                                        </SelectContent>
                                    </Select>
                                </Field>
                                <Field>
                                    <FieldLabel>Password</FieldLabel>
                                    <Input value={password}
                                           onChange={(event) => setPassword(event.target.value)}
                                           type="password" />
                                </Field>
                                <Field>
                                    <FieldLabel>Description</FieldLabel>
                                    <Input value={description}
                                           onChange={(event) => setDescription(event.target.value)}
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
            <div className="mt-2 text-muted-foreground">Manage user accounts, view account details, and control platform access.</div>
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
                        users.map((user) => (
                            <TableRow key={user.name}>
                                <TableCell>{user.name}</TableCell>
                                <TableCell>{user.description}</TableCell>
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
                                                <DialogTitle>User Detail</DialogTitle>
                                            </DialogHeader>
                                            <FieldGroup>
                                                <Field>
                                                    <FieldLabel>Name</FieldLabel>
                                                    <div>{user.name}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>ID</FieldLabel>
                                                    <div>{user.id}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Role ID</FieldLabel>
                                                    <div>{user.roleId}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Description</FieldLabel>
                                                    <div>{user.description}</div>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Last Login At</FieldLabel>
                                                    <div>{user.lastLoginAt ?? "N/A"}</div>
                                                </Field>
                                            </FieldGroup>
                                        </DialogContent>
                                    </Dialog>
                                    <Dialog open={openEditing} onOpenChange={setOpenEditing}>
                                        <DialogTrigger render={
                                            <Button className="w-32 mr-6" variant="outline" onClick={() => setUpdatingInfo(user)}>
                                                <PiNotePencil />
                                                Edit
                                            </Button>
                                        } />
                                        <DialogContent>
                                            <DialogHeader>
                                                <DialogTitle>Edit User</DialogTitle>
                                            </DialogHeader>
                                            <FieldGroup>
                                                <Field>
                                                    <FieldLabel>Name</FieldLabel>
                                                    <Input value={updatedName}
                                                           onChange={(event) => setUpdatedName(event.target.value)}
                                                           type="text" />
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Role</FieldLabel>
                                                    <Select value={updatedRole}
                                                            onValueChange={(newValue) => setUpdatedRole(newValue ?? "")}
                                                            items={(rolesQuery.data?.items ?? []).map(role => ({value: role.id, label: role.name}))}>
                                                        <SelectTrigger className="w-full max-w-48">
                                                            <SelectValue />
                                                        </SelectTrigger>
                                                        <SelectContent>
                                                            <SelectGroup>
                                                                <SelectLabel>Roles</SelectLabel>
                                                                {
                                                                    rolesQuery.data?.items.map((role) => (
                                                                        <SelectItem key={role.id} value={role.id}>
                                                                            {role.name}
                                                                        </SelectItem>
                                                                    ))
                                                                }
                                                            </SelectGroup>
                                                        </SelectContent>
                                                    </Select>
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Password</FieldLabel>
                                                    <Input value={updatedPassword}
                                                           onChange={(event) => setUpdatedPassword(event.target.value)}
                                                           type="password" />
                                                </Field>
                                                <Field>
                                                    <FieldLabel>Description</FieldLabel>
                                                    <Input value={updatedDescription}
                                                           onChange={(event) => setUpdatedDescription(event.target.value)}
                                                           type="text" />
                                                </Field>
                                            </FieldGroup>
                                            <DialogFooter>
                                                <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                                <Button type="submit" onClick={update} disabled={updating}>Update</Button>
                                            </DialogFooter>
                                        </DialogContent>
                                    </Dialog>
                                    <Button className="w-32" variant="destructive" onClick={() => del(user.id)} disabled={deleting}>
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