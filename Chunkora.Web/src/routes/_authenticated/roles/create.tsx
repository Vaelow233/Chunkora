import {createFileRoute, Link, useNavigate} from '@tanstack/react-router'
import {useEffect, useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {createRole} from "@/api/role.ts";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList, BreadcrumbPage,
    BreadcrumbSeparator
} from "@/components/ui/breadcrumb.tsx";
import {Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {Button} from "@/components/ui/button.tsx";
import {PermissionItem} from "@/components/PermissionItem";
import {PiShieldPlus} from "react-icons/pi";
import {listInstances} from "@/api/instance.ts";
import {listNodes} from "@/api/node.ts";
import { useQuery } from "@tanstack/react-query";
import {createPermissionTree} from "@/utils/permission.ts";

export const Route = createFileRoute('/_authenticated/roles/create')({
  component: RoleCreationPage,
})

function RoleCreationPage() {
    const navigate = useNavigate()
    
    const [name, setName] = useState("")
    const [description, setDescription] = useState("")
    const [permissions, setPermissions] = useState<string[]>([])

    const [creating, setCreating] = useState(false)

    const nodesQuery = useQuery({
        queryKey: ["nodes"],
        queryFn: () => listNodes(0, 2147483647),
    })

    const instancesQuery = useQuery({
        queryKey: ["instances"],
        queryFn: () => listInstances(0, 2147483647),
    })

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
    
    const create = async () => {
        if (!name) {
            toast.add({
                type: "error",
                title: "Failed to create role.",
                description: "The name of the role should be non-empty.",
            })
            return
        }
        setCreating(true)
        try {
            await createRole({
                name: name,
                permissions: permissions,
                description: description,
            })
            toast.add({
                type: "success",
                title: `Create role ${name} successfully.`,
            })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create role.",
                description: (e instanceof Error ? e.message : String(e)),
            })
            return
        } finally {
            setCreating(false)
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
                        <BreadcrumbPage>Creation</BreadcrumbPage>
                    </BreadcrumbItem>
                </BreadcrumbList>
            </Breadcrumb>
            <div className="flex flex-row mt-4 gap-4 items-center">
                <div className="w-10 h-10 bg-primary-foreground flex justify-center items-center rounded-lg">
                    <PiShieldPlus className="w-6 h-6 text-primary" />
                </div>
                <div className="flex flex-col text-2xl font-bold">Create New Role</div>
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
                <Button type="submit" className="mt-4 ml-auto w-32" onClick={create} disabled={creating}>Create</Button>
            </div>
        </div>
    )
}
