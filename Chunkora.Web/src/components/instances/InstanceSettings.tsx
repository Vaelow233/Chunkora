import type {InstanceDto} from "@/types/instance.ts";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {PiGauge, PiPlus, PiSliders, PiTrash} from "react-icons/pi";
import {Field, FieldDescription, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {useEffect, useState} from "react";
import {Input} from "@/components/ui/input.tsx";
import {Button} from "@/components/ui/button.tsx";
import {toast} from "@/components/ui/toast.tsx";
import {updateInstance, updateInstanceDockerConfig} from "@/api/instance.ts";
import {useQuery} from "@tanstack/react-query";
import {listNodes} from "@/api/node.ts";
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
    Dialog, DialogContent,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table.tsx";
import {CreatableCombobox} from "@/components/CreatableCombobox.tsx";
import {Switch} from "@/components/ui/switch.tsx";

interface InstanceSettingsProps {
    id: string
    instance: InstanceDto | undefined
}

interface IdNamePair {
    id: number
    name: string
}

const instanceTypes: IdNamePair[] = [{name: "Host", id: 0}, {name: "Docker", id: 1}]

const restartPolicies: IdNamePair[] = [{name: "Undefined", id: 0}, {name: "No", id: 1}, {name: "Always", id: 2}, {name: "On failure", id: 3}, {name: "Unless stopped", id: 4}]

const protocols: IdNamePair[] = [{name: "TCP", id: 1}, {name: "UDP", id: 2}]

interface PortBind {
    id: string
    containerPort: number
    containerProtocol: number
    hostIp: string
    hostPort: number
}

interface FileBind {
    id: string
    hostPath: string
    containerPath: string
    readOnly: boolean
}

export function InstanceSettings({id, instance}: InstanceSettingsProps) {
    const [nodeId, setNodeId] = useState(instance?.nodeId ?? "")
    const [type, setType] = useState(instance?.type ?? 0)
    const [name, setName] = useState(instance?.name ?? "")
    const [description, setDescription] = useState(instance?.description ?? "")
    const [path, setPath] = useState(instance?.path ?? "")
    const [launchCommand, setLaunchCommand] = useState(instance?.launchCommand ?? "")
    const [stopCommand, setStopCommand] = useState(instance?.stopCommand ?? "")
    
    const [updating, setUpdating] = useState(false)
    
    const [updatingDocker, setUpdatingDocker] = useState(false)
    const [containerName, setContainerName] = useState(instance?.dockerConfig?.name ?? "")
    const [containerImage, setContainerImage] = useState(instance?.dockerConfig?.image ?? "")
    const [containerEnvs, setContainerEnvs] = useState(instance?.dockerConfig?.environments ?? [])
    const [containerWorkingDir, setContainerWorkingDir] = useState(instance?.dockerConfig?.workingDirectory ?? "")
    const [containerHostname, setContainerHostname] = useState(instance?.dockerConfig?.hostname ?? "")
    const [containerMaxMemoryBytes, setContainerMaxMemoryBytes] = useState(instance?.dockerConfig?.maxMemoryBytes ?? 0)
    const [containerMaxNanoCpus, setContainerMaxNanoCpus] = useState(instance?.dockerConfig?.maxNanoCpus ?? 0)
    const [containerNetworkMode, setContainerNetworkMode] = useState(instance?.dockerConfig?.networkMode ?? "")
    const [containerRestartPolicy, setContainerRestartPolicy] = useState(instance?.dockerConfig?.restartPolicy ?? 0)
    const [containerPortBinds, setContainerPortBinds] = useState<PortBind[]>(instance?.dockerConfig?.portBindings.map(bind => ({...bind, id: crypto.randomUUID()})) ?? [])
    const [containerFileBinds, setContainerFileBinds] = useState<FileBind[]>(instance?.dockerConfig?.fileBindings.map(bind => ({...bind, id: crypto.randomUUID()})) ?? [])
    
    const [openEditPortBindings, setOpenEditPortBindings] = useState(false)
    const [openEditFileBindings, setOpenEditFileBindings] = useState(false)

    const nodesQuery = useQuery({
        queryKey: ["nodes"],
        queryFn: () => listNodes(0, 2147483647),
    })

    useEffect(() => {
        if (!nodesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch nodes data. You may not update the node of instance because there are no nodes to choose.",
            description: nodesQuery.error.message,
        })
    }, [nodesQuery.error])

    const updatePortBind = (id: string, patch: Partial<PortBind>) => setContainerPortBinds(prev => 
        prev.map(b => (b.id === id ? { ...b, ...patch } : b)))

    const addPortBind = () => setContainerPortBinds(prev =>
        [...prev, { id: crypto.randomUUID(), containerPort: 0, hostPort: 0, hostIp: "", containerProtocol: 1 }])

    const removePortBind = (id: string) => setContainerPortBinds(prev => prev.filter(b => b.id !== id))

    const updateFileBind = (id: string, patch: Partial<FileBind>) => setContainerFileBinds(prev =>
        prev.map(b => (b.id === id ? { ...b, ...patch } : b)))

    const addFileBind = () => setContainerFileBinds(prev => 
        [...prev, { id: crypto.randomUUID(), containerPath: "", hostPath: "", readOnly: true }])

    const removeFileBind = (id: string) => setContainerFileBinds(prev => prev.filter(b => b.id !== id))
    
    const update = async () => {
        setUpdating(true)
        try {
            await updateInstance({
                id: id,
                nodeId: nodeId,
                name: name,
                description: description,
                path: path,
                launchCommand: launchCommand,
                stopCommand: stopCommand,
                type: type,
            })
            toast.add({
                type: "success",
                title: `Update instance ${name} successfully.`,
            })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdating(false)
        }
    }
    
    const updateDocker = async () => {
        setUpdatingDocker(true)
        try {
            await updateInstanceDockerConfig({
                id: id,
                name: containerName,
                image: containerImage,
                environments: containerEnvs,
                workingDirectory: containerWorkingDir,
                hostname: containerHostname,
                maxMemoryBytes: containerMaxMemoryBytes,
                maxNanoCpus: containerMaxNanoCpus,
                networkMode: containerNetworkMode,
                restartPolicy: containerRestartPolicy,
                portBindings: containerPortBinds,
                fileBindings: containerFileBinds,
            })
            toast.add({
                type: "success",
                title: `Update the docker config of instance ${name} successfully.`,
            })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update docker config of instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdatingDocker(false)
        }
    }
    
    return (
        <div className="flex flex-col gap-8">
            <Card>
                <CardHeader>
                    <CardTitle>
                        <div className="flex flex-row items-center text-2xl font-bold gap-4">
                            <div className="w-10 h-10 bg-primary-foreground flex justify-center items-center rounded-lg">
                                <PiSliders className="w-6 h-6 text-primary" />
                            </div>
                            General
                        </div>
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <FieldGroup>
                        <div className="flex flex-row gap-4">
                            <Field>
                                <FieldLabel>Node</FieldLabel>
                                <Select value={nodeId}
                                        onValueChange={(newValue) => setNodeId(newValue ?? "")}
                                        items={(nodesQuery.data?.items ?? []).map(node => ({value: node.id, label: node.name}))}
                                >
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectGroup>
                                            <SelectLabel>Nodes</SelectLabel>
                                            {
                                                nodesQuery.data?.items.map((node) => (
                                                    <SelectItem key={node.id} value={node.id}>
                                                        {node.name}
                                                    </SelectItem>
                                                ))
                                            }
                                        </SelectGroup>
                                    </SelectContent>
                                </Select>
                                <FieldDescription>The node where this instance is hosted. Moving an instance to another node does not automatically migrate its data.</FieldDescription>
                            </Field>
                            <Field>
                                <FieldLabel>Name</FieldLabel>
                                <Input value={name}
                                       onChange={(e) => setName(e.target.value)}
                                       type="text"
                                />
                                <FieldDescription>The name used to identify this instance.</FieldDescription>
                            </Field>
                            <Field>
                                <FieldLabel>Type</FieldLabel>
                                <Select value={type}
                                        onValueChange={(newValue) => setType(Number(newValue) ?? 0)}
                                        items={instanceTypes.map(node => ({value: node.id, label: node.name}))}
                                >
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectGroup>
                                            <SelectLabel>Type</SelectLabel>
                                            {
                                                instanceTypes.map((node) => (
                                                    <SelectItem key={node.id} value={node.id}>
                                                        {node.name}
                                                    </SelectItem>
                                                ))
                                            }
                                        </SelectGroup>
                                    </SelectContent>
                                </Select>
                                <FieldDescription>The type of the instance. Use Docker container isolation for better safety.</FieldDescription>
                            </Field>
                        </div>
                        <Field>
                            <FieldLabel>Description</FieldLabel>
                            <Input value={description}
                                   onChange={(e) => setDescription(e.target.value)}
                                   type="text"
                            />
                            <FieldDescription>The description of this instance.</FieldDescription>
                        </Field>
                        <Field>
                            <FieldLabel>Path</FieldLabel>
                            <Input value={path}
                                   onChange={(e) => setPath(e.target.value)}
                                   type="text"
                            />
                            <FieldDescription>The path to the instance's working directory.</FieldDescription>
                        </Field>
                        <div className="flex flex-row gap-4">
                            <Field>
                                <FieldLabel>Launch Command</FieldLabel>
                                <Input value={launchCommand}
                                       onChange={(e) => setLaunchCommand(e.target.value)}
                                       type="text"
                                />
                                <FieldDescription>The command used to launch this instance.</FieldDescription>
                            </Field>
                            <Field>
                                <FieldLabel>Stop Command</FieldLabel>
                                <Input value={stopCommand}
                                       onChange={(e) => setStopCommand(e.target.value)}
                                       type="text"
                                />
                                <FieldDescription>The command sent to the instance's standard input (stdin) to stop it.</FieldDescription>
                            </Field>
                        </div>
                    </FieldGroup>
                    <div className="flex flex-row mt-10">
                        <Button type="submit" className="ml-auto w-32" onClick={update} disabled={updating}>Apply Changes</Button>
                    </div>
                </CardContent>
            </Card>
            {
                type !== 1 ? <></> : (
                    <Card>
                        <CardHeader>
                            <CardTitle>
                                <div className="flex flex-row items-center text-2xl font-bold gap-4">
                                    <div className="w-10 h-10 bg-secondary-foreground flex justify-center items-center rounded-lg">
                                        <PiGauge className="w-6 h-6 text-secondary" />
                                    </div>
                                    Docker Configuration
                                </div>
                            </CardTitle>
                        </CardHeader>
                        <CardContent>
                            <FieldGroup>
                                <div className="flex flex-row gap-4">
                                    <Field>
                                        <FieldLabel>Name</FieldLabel>
                                        <Input value={containerName}
                                               onChange={(e) => setContainerName(e.target.value)}
                                               type="text"
                                        />
                                        <FieldDescription>The name of the container in Docker.</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>Image</FieldLabel>
                                        <Input value={containerImage}
                                               onChange={(e) => setContainerImage(e.target.value)}
                                               type="text"
                                        />
                                        <FieldDescription>The image used in the container.</FieldDescription>
                                    </Field>
                                </div>
                                <div className="flex flex-row gap-4">
                                    <Field>
                                        <FieldLabel>Hostname</FieldLabel>
                                        <Input value={containerHostname}
                                               onChange={(e) => setContainerHostname(e.target.value)}
                                               type="text"
                                        />
                                        <FieldDescription>The hostname of the container.</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>Network Mode</FieldLabel>
                                        <Input value={containerNetworkMode}
                                               onChange={(e) => setContainerNetworkMode(e.target.value)}
                                               type="text"
                                        />
                                        <FieldDescription>The network mode of the container. In Docker, the default network mode is bridge.</FieldDescription>
                                    </Field>
                                </div>
                                <div className="flex flex-row gap-4">
                                    <Field>
                                        <FieldLabel>Restart Policy</FieldLabel>
                                        <Select value={containerRestartPolicy}
                                                onValueChange={(newValue) => setContainerRestartPolicy(Number(newValue) ?? 0)}
                                                items={restartPolicies.map(node => ({value: node.id, label: node.name}))}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectGroup>
                                                    <SelectLabel>Type</SelectLabel>
                                                    {
                                                        restartPolicies.map((node) => (
                                                            <SelectItem key={node.id} value={node.id}>
                                                                {node.name}
                                                            </SelectItem>
                                                        ))
                                                    }
                                                </SelectGroup>
                                            </SelectContent>
                                        </Select>
                                        <FieldDescription>The restart policy of the container.</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>Max Nano CPUs</FieldLabel>
                                        <Input value={containerMaxNanoCpus}
                                               onChange={(e) => setContainerMaxNanoCpus(Number(e.target.value))}
                                               type="number"
                                        />
                                        <FieldDescription>The maximum CPU nano time allocated to the container, i.e. if you want to limit the container to use up to 1 core of the CPU, fill 1000000000 (1 * 10⁹)</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>Max Memory Bytes</FieldLabel>
                                        <Input value={containerMaxMemoryBytes}
                                               onChange={(e) => setContainerMaxMemoryBytes(Number(e.target.value))}
                                               type="number"
                                        />
                                        <FieldDescription>The maximum memory bytes allocated to the container, i.e. if you want to limit the container to use up to 1 GB memory, fill 1073741824 (1 * 1024³)</FieldDescription>
                                    </Field>
                                </div>
                                <Field>
                                    <FieldLabel>Working Directory</FieldLabel>
                                    <Input value={containerWorkingDir}
                                           onChange={(e) => setContainerWorkingDir(e.target.value)}
                                           type="text"
                                    />
                                    <FieldDescription>The path to the container's working directory.</FieldDescription>
                                </Field>
                                <div className="flex flex-row gap-4">
                                    <Field>
                                        <FieldLabel>Environments</FieldLabel>
                                        <CreatableCombobox value={containerEnvs} onChange={(value) => setContainerEnvs(value)} options={[]} />
                                        <FieldDescription>Configure environment variables that are passing to the container.</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>Port Bindings</FieldLabel>
                                        <Dialog open={openEditPortBindings} onOpenChange={setOpenEditPortBindings}>
                                            <DialogTrigger render={
                                                <div className="mr-auto">
                                                    <Button variant="outline" className="min-w-24">
                                                        Configure
                                                    </Button>
                                                </div>
                                            } />
                                            <DialogContent className="min-w-3xl">
                                                <DialogHeader>
                                                    <DialogTitle>Configure Port Bindings</DialogTitle>
                                                </DialogHeader>
                                                <Button className="w-32 mt-4" onClick={() => addPortBind()}>
                                                    <PiPlus />
                                                    Create
                                                </Button>
                                                <Table>
                                                    <TableHeader>
                                                        <TableRow>
                                                            <TableHead>Container Port</TableHead>
                                                            <TableHead>Host IP</TableHead>
                                                            <TableHead>Host Port</TableHead>
                                                            <TableHead className="w-32">Protocol</TableHead>
                                                            <TableHead className="w-16">Action</TableHead>
                                                        </TableRow>
                                                    </TableHeader>
                                                    <TableBody>
                                                        {
                                                            containerPortBinds.map((bind) => (
                                                                <TableRow key={bind.id}>
                                                                    <TableCell>
                                                                        <Input value={bind.containerPort}
                                                                               onChange={(event) => updatePortBind(bind.id, { containerPort: Number(event.target.value) })}
                                                                               type="number" 
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Input value={bind.hostIp}
                                                                               onChange={(event) => updatePortBind(bind.id, { hostIp: event.target.value })}
                                                                               type="text"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Input value={bind.hostPort}
                                                                               onChange={(event) => updatePortBind(bind.id, { hostPort: Number(event.target.value) })}
                                                                               type="number"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Select value={bind.containerProtocol}
                                                                                onValueChange={(newValue) => updatePortBind(bind.id, { containerProtocol: newValue ?? 0 })}
                                                                                items={protocols.map(node => ({value: node.id, label: node.name}))}
                                                                        >
                                                                            <SelectTrigger>
                                                                                <SelectValue />
                                                                            </SelectTrigger>
                                                                            <SelectContent>
                                                                                <SelectGroup>
                                                                                    <SelectLabel>Protocol</SelectLabel>
                                                                                    {
                                                                                        protocols.map((node) => (
                                                                                            <SelectItem key={node.id} value={node.id}>
                                                                                                {node.name}
                                                                                            </SelectItem>
                                                                                        ))
                                                                                    }
                                                                                </SelectGroup>
                                                                            </SelectContent>
                                                                        </Select>
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Button variant="destructive" size="icon" onClick={() => removePortBind(bind.id)}>
                                                                            <PiTrash />
                                                                        </Button>
                                                                    </TableCell>
                                                                </TableRow>
                                                            ))
                                                        }
                                                    </TableBody>
                                                </Table>
                                            </DialogContent>
                                        </Dialog>
                                        <FieldDescription>Configure ports that are exposed to the host from the container.</FieldDescription>
                                    </Field>
                                    <Field>
                                        <FieldLabel>File Bindings</FieldLabel>
                                        <Dialog open={openEditFileBindings} onOpenChange={setOpenEditFileBindings}>
                                            <DialogTrigger render={
                                                <div className="mr-auto">
                                                    <Button variant="outline" className="min-w-24">
                                                        Configure
                                                    </Button>
                                                </div>
                                            } />
                                            <DialogContent className="min-w-3xl">
                                                <DialogHeader>
                                                    <DialogTitle>Configure File Bindings</DialogTitle>
                                                </DialogHeader>
                                                <Button className="w-32 mt-4" onClick={() => addFileBind()}>
                                                    <PiPlus />
                                                    Create
                                                </Button>
                                                <Table>
                                                    <TableHeader>
                                                        <TableRow>
                                                            <TableHead>Container Path</TableHead>
                                                            <TableHead>Host Path</TableHead>
                                                            <TableHead className="w-16">Read Only</TableHead>
                                                            <TableHead className="w-16">Action</TableHead>
                                                        </TableRow>
                                                    </TableHeader>
                                                    <TableBody>
                                                        {
                                                            containerFileBinds.map((bind) => (
                                                                <TableRow key={bind.id}>
                                                                    <TableCell>
                                                                        <Input value={bind.containerPath}
                                                                               onChange={(event) => updateFileBind(bind.id, { containerPath: event.target.value })}
                                                                               type="text"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Input value={bind.hostPath}
                                                                               onChange={(event) => updateFileBind(bind.id, { hostPath: event.target.value })}
                                                                               type="text"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Switch checked={bind.readOnly}
                                                                                onCheckedChange={checked => updateFileBind(bind.id, { readOnly: checked })}
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Button variant="destructive" size="icon" onClick={() => removeFileBind(bind.id)}>
                                                                            <PiTrash />
                                                                        </Button>
                                                                    </TableCell>
                                                                </TableRow>
                                                            ))
                                                        }
                                                    </TableBody>
                                                </Table>
                                            </DialogContent>
                                        </Dialog>
                                        <FieldDescription>Configure files that are exposed to the container from the host.</FieldDescription>
                                    </Field>
                                </div>
                            </FieldGroup>
                            <div className="flex flex-row mt-10">
                                <Button type="submit" className="ml-auto w-32" variant="outline" onClick={updateDocker} disabled={updatingDocker}>Apply Changes</Button>
                            </div>
                        </CardContent>
                    </Card>
                )
            }
        </div>
    )
}