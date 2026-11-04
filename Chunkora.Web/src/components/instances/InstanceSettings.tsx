import type {InstanceDto} from "@/types/instance.ts";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {PiSliders} from "react-icons/pi";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {useState} from "react";
import {Input} from "@/components/ui/input.tsx";
import {Button} from "@/components/ui/button.tsx";
import {toast} from "@/components/ui/toast.tsx";
import {updateInstance} from "@/api/instance.ts";

interface InstanceSettingsProps {
    id: string
    instance: InstanceDto | undefined
}

export function InstanceSettings({id, instance}: InstanceSettingsProps) {
    const [nodeId, setNodeId] = useState(instance?.nodeId ?? "")
    const [name, setName] = useState(instance?.name ?? "")
    const [description, setDescription] = useState(instance?.description ?? "")
    const [path, setPath] = useState(instance?.path ?? "")
    const [launchCommand, setLaunchCommand] = useState(instance?.launchCommand ?? "")
    const [stopCommand, setStopCommand] = useState(instance?.stopCommand ?? "")
    
    const [updating, setUpdating] = useState(false)
    
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
            })
            toast.add({
                type: "success",
                title: `Update node ${name} successfully.`,
            })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update node.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdating(false)
        }
    }
    
    return (
        <div className="flex flex-col gap-4">
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
                                <FieldLabel>Node ID</FieldLabel>
                                <div>{nodeId}</div>
                            </Field>
                            <Field>
                                <FieldLabel>Name</FieldLabel>
                                <Input value={name}
                                       onChange={(e) => setName(e.target.value)}
                                       type="text"
                                />
                            </Field>
                        </div>
                        <Field>
                            <FieldLabel>Description</FieldLabel>
                            <Input value={description}
                                   onChange={(e) => setDescription(e.target.value)}
                                   type="text"
                            />
                        </Field>
                        <Field>
                            <FieldLabel>Path</FieldLabel>
                            <Input value={path}
                                   onChange={(e) => setPath(e.target.value)}
                                   type="text"
                            />
                        </Field>
                        <div className="flex flex-row gap-4">
                            <Field>
                                <FieldLabel>Launch Command</FieldLabel>
                                <Input value={launchCommand}
                                       onChange={(e) => setLaunchCommand(e.target.value)}
                                       type="text"
                                />
                            </Field>
                            <Field>
                                <FieldLabel>Stop Command</FieldLabel>
                                <Input value={stopCommand}
                                       onChange={(e) => setStopCommand(e.target.value)}
                                       type="text"
                                />
                            </Field>
                        </div>
                    </FieldGroup>
                    <div className="flex flex-row mt-10">
                        <Button type="submit" className="ml-auto w-32" onClick={update} disabled={updating}>Apply Changes</Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    )
}