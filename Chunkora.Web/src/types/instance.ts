export interface InstanceDto {
    id: string
    nodeId: string
    name: string 
    description: string
    path: string
    launchCommand: string
    stopCommand: string
    status: number
    type: number
    dockerConfig: DockerConfig | null
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
    type: number
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

export interface DockerPortBinding {
    containerPort: number
    containerProtocol: number
    hostIp: string
    hostPort: number
}

export interface DockerFileBinding {
    hostPath: string
    containerPath: string
    readOnly: boolean
}

export interface DockerConfig {
    name: string
    image: string
    environments: string[]
    workingDirectory: string
    hostname: string
    maxMemoryBytes: number
    maxNanoCpus: number
    networkMode: string
    restartPolicy: number
    portBindings: DockerPortBinding[]
    fileBindings: DockerFileBinding[]
}

export interface UpdateInstanceDockerConfigRequest extends DockerConfig {
    id: string
}