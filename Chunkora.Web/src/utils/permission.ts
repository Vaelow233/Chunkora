import type {NodeDto} from "@/types/node.ts";
import type {InstanceDto} from "@/types/instance.ts";

export interface PermissionNode {
    name: string
    permission?: string
    description?: string
    children?: PermissionNode[]
}

export function createPermissionTree(nodes: NodeDto[], instances: InstanceDto[]) {
    // Prepare nodes (admin) permission tree
    const nodeAdminPerms: PermissionNode[] = nodes.map(item => {
        return {
            name: `admin.node.${item.id}`,
            description: `Admin permissions for node "${item.name}"`,
            permission: `admin.node.${item.id}.*`,
            children: [
                {
                    name: `admin.node.${item.id}.get`,
                    permission: `admin.node.${item.id}.get`,
                },
                {
                    name: `admin.node.${item.id}.get.secure`,
                    permission: `admin.node.${item.id}.get.secure`,
                },
                {
                    name: `admin.node.${item.id}.status`,
                    permission: `admin.node.${item.id}.status`,
                },
                {
                    name: `admin.node.${item.id}.metrics`,
                    permission: `admin.node.${item.id}.metrics`,
                },
                {
                    name: `admin.node.${item.id}.update`,
                    permission: `admin.node.${item.id}.update`,
                },
                {
                    name: `admin.node.${item.id}.delete`,
                    permission: `admin.node.${item.id}.delete`,
                },
                {
                    name: `admin.node.${item.id}.instance`,
                    permission: `admin.node.${item.id}.instance.*`,
                    children: [
                        {
                            name: `admin.node.${item.id}.instance.create`,
                            permission: `admin.node.${item.id}.instance.create`,
                        }
                    ]
                },
            ]
        }
    })
    
    // Prepare instances (admin) permission tree
    const instanceAdminPerms: PermissionNode[] = instances.map(item => {
        return {
            name: `admin.instance.${item.id}`,
            description: `Admin permissions for instance "${item.name}"`,
            permission: `admin.instance.${item.id}.*`,
            children: [
                {
                    name: `admin.instance.${item.id}.delete`,
                    permission: `admin.instance.${item.id}.delete`,
                },
                {
                    name: `admin.instance.${item.id}.update`,
                    permission: `admin.instance.${item.id}.update`,
                },
            ]
        }
    })
    
    // Prepare instances permission tree
    const instancePerms: PermissionNode[] = instances.map(item => {
        return {
            name: `instance.${item.id}`,
            description: `Basic permissions for instance "${item.name}"`,
            permission: `instance.${item.id}.*`,
            children: [
                {
                    name: `instance.${item.id}.get`,
                    permission: `instance.${item.id}.get`,
                },
                {
                    name: `instance.${item.id}.start`,
                    permission: `instance.${item.id}.start`,
                },
                {
                    name: `instance.${item.id}.stop`,
                    permission: `instance.${item.id}.stop`,
                },
                {
                    name: `instance.${item.id}.restart`,
                    permission: `instance.${item.id}.restart`,
                },
                {
                    name: `instance.${item.id}.kill`,
                    permission: `instance.${item.id}.kill`,
                },
                {
                    name: `instance.${item.id}.execute`,
                    permission: `instance.${item.id}.execute`,
                },
                {
                    name: `instance.${item.id}.logs`,
                    permission: `instance.${item.id}.logs`,
                },
                /* permissions about file operations */
                {
                    name: `instance.${item.id}.files`,
                    description: `Basic permissions of file operations for instance "${item.name}"`,
                    permission: `instance.${item.id}.files.*`,
                    children: [
                        {
                            name: `instance.${item.id}.files.list`,
                            permission: `instance.${item.id}.files.list`,
                        },
                        {
                            name: `instance.${item.id}.files.read`,
                            permission: `instance.${item.id}.files.read`,
                        },
                        {
                            name: `instance.${item.id}.files.download`,
                            permission: `instance.${item.id}.files.download`,
                        },
                        {
                            name: `instance.${item.id}.files.upload`,
                            permission: `instance.${item.id}.files.upload`,
                        },
                        {
                            name: `instance.${item.id}.files.create`,
                            permission: `instance.${item.id}.files.create`,
                        },
                        {
                            name: `instance.${item.id}.files.delete`,
                            permission: `instance.${item.id}.files.delete`,
                        },
                        {
                            name: `instance.${item.id}.files.copy`,
                            permission: `instance.${item.id}.files.copy`,
                        },
                        {
                            name: `instance.${item.id}.files.move`,
                            permission: `instance.${item.id}.files.move`,
                        },
                        {
                            name: `instance.${item.id}.files.rename`,
                            permission: `instance.${item.id}.files.rename`,
                        },
                    ]
                },
                /* permissions about directory operations */
                {
                    name: `instance.${item.id}.directories`,
                    description: `Basic permissions of directory operations for instance "${item.name}"`,
                    permission: `instance.${item.id}.directories.*`,
                    children: [
                        {
                            name: `instance.${item.id}.directories.create`,
                            permission: `instance.${item.id}.directories.create`,
                        },
                        {
                            name: `instance.${item.id}.directories.delete`,
                            permission: `instance.${item.id}.directories.delete`,
                        },
                        {
                            name: `instance.${item.id}.directories.copy`,
                            permission: `instance.${item.id}.directories.copy`,
                        },
                        {
                            name: `instance.${item.id}.directories.move`,
                            permission: `instance.${item.id}.directories.move`,
                        },
                        {
                            name: `instance.${item.id}.directories.rename`,
                            permission: `instance.${item.id}.directories.rename`,
                        },
                    ]
                },
            ]
        }
    })
    
    // Prepare the final permission tree
    const permissionTree: PermissionNode = {
        name: "[ root ]",
        description: "*",
        permission: "*",
        children: [
            {
                name: "admin",
                description: "admin.*",
                permission: "admin.*",
                children: [
                    {
                        name: "admin.user",
                        description: "admin.user.*",
                        permission: "admin.user.*",
                        children: [
                            {
                                name: "admin.user.create",
                                permission: "admin.user.create",
                            },
                            {
                                name: "admin.user.update",
                                permission: "admin.user.update",
                            },
                            {
                                name: "admin.user.delete",
                                permission: "admin.user.delete",
                            },
                            {
                                name: "admin.user.get",
                                permission: "admin.user.get",
                            },
                            {
                                name: "admin.user.list",
                                permission: "admin.user.list",
                            },
                        ],
                    },
                    {
                        name: "admin.role",
                        description: "admin.role.*",
                        permission: "admin.role.*",
                        children: [
                            {
                                name: "admin.role.create",
                                permission: "admin.role.create",
                            },
                            {
                                name: "admin.role.update",
                                permission: "admin.role.update",
                            },
                            {
                                name: "admin.role.delete",
                                permission: "admin.role.delete",
                            },
                            {
                                name: "admin.role.get",
                                permission: "admin.role.get",
                            },
                            {
                                name: "admin.role.list",
                                permission: "admin.role.list",
                            },
                        ],
                    },
                    {
                        name: "admin.node",
                        description: "admin.node.*",
                        permission: "admin.node.*",
                        children: [
                            {
                                name: "admin.node.create",
                                permission: "admin.node.create",
                            },
                            ...nodeAdminPerms,
                        ]
                    },
                    {
                        name: "admin.instance",
                        description: "admin.instance.*",
                        permission: "admin.instance.*",
                        children: instanceAdminPerms,
                    },
                ],
            },
            {
                name: "instance",
                description: "instance.*",
                permission: "instance.*",
                children: instancePerms,
            }
        ]
    }
    
    return permissionTree
}
