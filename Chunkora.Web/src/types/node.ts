export interface NodeDto {
    id: string
    name: string
    description: string
}

export interface NodeSecureDto {
    id: string
    name: string
    description: string
    agentToken: string
}

export interface CreateNodeRequest {
    name: string
    description: string
    agentToken: string
}

export interface UpdateNodeRequest extends CreateNodeRequest {
    id: string
}

export interface NodeMetricsDto {
    id: string
    nodeId: string
    version: number
    architecture: string
    osType: string
    hostname: string
    osDetail: string
    cpuUsage: string
    memoryUsedBytes: number
    memoryTotalBytes: number
    createdAt: string
}

export interface NodeStatusDto {
    nodeId: string
    instanceConnected: boolean
    instanceOperationConnected: boolean
}