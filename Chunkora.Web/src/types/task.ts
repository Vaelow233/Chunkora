export interface InstanceTaskDto {
    id: string
    nodeId: string
    instanceId: string
    name: string
    description: string
    trigger: number
    triggerInfo: string
    action: number
    actionInfo: string
    lastRunAt: string
    nextRunAt: string
}

export interface CreateInstanceTaskRequest {
    nodeId: string
    instanceId: string
    name: string
    description: string
    trigger: number
    triggerInfo: string
    action: number
    actionInfo: string
}

export interface UpdateInstanceTaskRequest extends CreateInstanceTaskRequest {
    id: string
}