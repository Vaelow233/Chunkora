import {createFileRoute, Link, useNavigate} from '@tanstack/react-router'
import {useEffect, useState} from "react";
import {useQuery} from "@tanstack/react-query";
import {listNodes} from "@/api/node.ts";
import {listInstances} from "@/api/instance.ts";
import {toast} from "@/components/ui/toast.tsx";
import {createPermissionTree} from "@/utils/permission.ts";
import {getRole, updateRole} from "@/api/role.ts";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList, BreadcrumbPage,
    BreadcrumbSeparator
} from "@/components/ui/breadcrumb.tsx";
import {PiShieldPlus} from "react-icons/pi";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {PermissionItem} from "@/components/PermissionItem.tsx";
import {Button} from "@/components/ui/button.tsx";

export const Route = createFileRoute('/_authenticated/roles/update/$id')({
    component: RoleUpdatePage,
})

function RoleUpdatePage() {
    const { id } = Route.useParams()

    const navigate = useNavigate()

    const [name, setName] = useState("")
    const [description, setDescription] = useState("")
    const [permissions, setPermissions] = useState<string[]>([])

    const [updating, setUpdating] = useState(false)
    
    const roleQuery = useQuery({
        queryKey: ["role", id],
        queryFn: () => getRole(id),
    })

    const nodesQuery = useQuery({
        queryKey: ["nodes"],
        queryFn: () => listNodes(0, 2147483647),
    })

    const instancesQuery = useQuery({
        queryKey: ["instances"],
        queryFn: () => listInstances(0, 2147483647),
    })

    useEffect(() => {
        if (!roleQuery.data) return

        setName(roleQuery.data.name)
        setDescription(roleQuery.data.description)
        setPermissions(roleQuery.data.permissions)
    }, [roleQuery.data])

    useEffect(() => {
        if (!roleQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to fetch role ${id} data.`,
            description: roleQuery.error.message,
        })
    }, [roleQuery.error])

    useEffect(() => {
        if (!nodesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch nodes data. You may not able to see the permission related to nodes.",
            description: nodesQuery.error.message,
        })
    }, [nodesQuery.error])

    useEffect(() => {
        if (!instancesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch instances data. You may not able to see the permission related to instances.",
            description: instancesQuery.error.message,
        })
    }, [instancesQuery.error])

    const permissionTree = createPermissionTree(nodesQuery.data?.items ?? [], instancesQuery.data?.items ?? [])

    const update = async () => {
        if (!name) {
            toast.add({
                type: "error",
                title: "Failed to update role.",
                description: "The name of the role should be non-empty.",
            })
            return
        }
        setUpdating(true)
        try {
            await updateRole({
                id: id,
                name: name,
                permissions: permissions,
                description: description,
            })
            toast.add({
                type: "success",
                title: `Update role ${name} successfully.`,
            })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update role.",
                description: (e instanceof Error ? e.message : String(e)),
            })
            return
        } finally {
            setUpdating(false)
        }
        await navigate({
            to: "/roles",
        })
    }

    return (
        <div className="m-8">
            <Breadcrumb>
                <BreadcrumbList>
                    <BreadcrumbItem>
                        <BreadcrumbLink render={<Link to="/roles"/>}>Roles</BreadcrumbLink>
                    </BreadcrumbItem>
                    <BreadcrumbSeparator />
                    <BreadcrumbItem>
                        <BreadcrumbPage>Update</BreadcrumbPage>
                    </BreadcrumbItem>
                </BreadcrumbList>
            </Breadcrumb>
            <div className="flex flex-row mt-4 gap-4 items-center">
                <div className="w-10 h-10 bg-primary-foreground flex justify-center items-center rounded-lg">
                    <PiShieldPlus className="w-6 h-6 text-primary" />
                </div>
                <div className="flex flex-col text-2xl font-bold">Update Role ${id}</div>
            </div>
            <FieldGroup className="max-w-300 mt-6">
                <Field>
                    <div className="flex flex-row gap-4">
                        <FieldLabel>Name</FieldLabel>
                        <Input value={name}
                               onChange={(event) => setName(event.target.value)}
                               type="text" />
                    </div>
                </Field>
                <Field>
                    <div className="flex flex-row gap-4">
                        <FieldLabel>Description</FieldLabel>
                        <Input value={description}
                               onChange={(event) => setDescription(event.target.value)}
                               type="text" />
                    </div>
                </Field>
                <Field>
                    <div className="flex flex-row gap-4 items-start">
                        <FieldLabel>Permissions</FieldLabel>
                        <PermissionItem
                            node={permissionTree}
                            checkInclude={(permission) => permissions.includes(permission)}
                            updateStatus={(permission, checked) => {
                                if (checked && !permissions.includes(permission)) {
                                    setPermissions([...permissions, permission]);
                                }
                                if (!checked && permissions.includes(permission)) {
                                    setPermissions(permissions.filter(p => p !== permission));
                                }
                            }}
                        />
                    </div>
                </Field>
            </FieldGroup>
            <div className="flex flex-row">
                <Button type="submit" className="mt-4 ml-auto w-32" onClick={update} disabled={updating}>Update</Button>
            </div>
        </div>
    )
}
