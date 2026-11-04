import {Input} from "@/components/ui/input.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import { InstanceTerminal } from "./InstanceTerminal";
import type {InstanceDto} from "@/types/instance.ts";
import { useState } from "react";
import { toast } from "../ui/toast";
import { executeCommand } from "@/api/instance";
import {PiPaperPlaneRight, PiTag} from "react-icons/pi";

interface InstanceTerminalProps {
    id: string
    instance: InstanceDto | undefined
}

export function InstanceOverview({id, instance}: InstanceTerminalProps) {
    const [command, setCommand] = useState("")

    const [sending, setSending] = useState(false)
    
    const sendCommand = async () => {
        if (!command) {
            toast.add({
                type: "error",
                title: "Failed to execute command.",
                description: "The command should be non-empty.",
            })
            return
        }
        setSending(true)
        try {
            await executeCommand(id, command)
            toast.add({
                type: "success",
                title: `Execute command "${command}" successfully.`,
            })
            setCommand("")
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to execute command.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setSending(false)
        }
    }
    
    return (
        <div className="grid grid-cols-4 gap-4">
            <div className="col-span-3">
                <InstanceTerminal id={id} className="max-h-160" />
                <div className="flex flex-row mt-4 gap-3">
                    <Input value={command}
                           onChange={(e) => setCommand(e.target.value)}
                           placeholder="Execute command..."
                    />
                    <Button onClick={sendCommand} disabled={sending}>
                        <PiPaperPlaneRight />
                    </Button>
                </div>
            </div>
            <div className="col-span-1">
                <Card>
                    <CardHeader>
                        <CardTitle>
                            <div className="flex flex-row">
                                <PiTag className="min-w-5 min-h-5 text-primary mr-2" />
                                Metadata
                            </div>
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <FieldGroup>
                            <Field>
                                <FieldLabel>Node ID</FieldLabel>
                                <div>{instance?.nodeId}</div>
                            </Field>
                            <Field>
                                <FieldLabel>Path</FieldLabel>
                                <div>{instance?.path}</div>
                            </Field>
                            <Field>
                                <FieldLabel>Launch Command</FieldLabel>
                                <div>{instance?.launchCommand}</div>
                            </Field>
                            <Field>
                                <FieldLabel>Stop Command</FieldLabel>
                                <div>{instance?.stopCommand}</div>
                            </Field>
                        </FieldGroup>
                    </CardContent>
                </Card>
            </div>
        </div>
    )
}