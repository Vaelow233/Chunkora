export interface InstanceDto {
    id: string
    nodeId: string
    name: string 
    description: string
    path: string
    launchCommand: string
    stopCommand: string
    status: number
}

export interface CreateInstanceRequest {
    nodeId: string
    name: string
    description: string
    path: string
    launchCommand: string
    stopCommand: string
}

export interface UpdateInstanceRequest extends CreateInstanceRequest {
    id: string
}

export interface FileObjectDto {
    type: string
    name: string
    sizeBytes: number
    createdAt: string
    lastUpdatedAt: string
    permission: string
}

export interface FileContentDto {
    path: string
    content: string
}