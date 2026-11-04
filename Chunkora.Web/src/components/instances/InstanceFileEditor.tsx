import {useQuery, useQueryClient} from "@tanstack/react-query";
import {readFile, uploadFile} from "@/api/instance.ts";
import {useEffect, useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {Card, CardContent, CardHeader} from "@/components/ui/card.tsx";
import {PiFile, PiFloppyDisk, PiX} from "react-icons/pi";
import {Button} from "@/components/ui/button.tsx";
import { Textarea } from "../ui/textarea";

interface InstanceFileEditorProps {
    id: string
    directory: string[]
    name: string
    className?: string
    closeCallback: () => void
}

export function InstanceFileEditor({id, directory, name, className, closeCallback}: InstanceFileEditorProps) {
    const fileQuery = useQuery({
        queryKey: ["instance", id, "file", directory, name],
        queryFn: () => readFile(id, [...directory, name].join("/")),
    })

    useEffect(() => {
        if (!fileQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to read file ${name}.`,
            description: fileQuery.error.message,
        })
    }, [fileQuery.error])

    function base64ToBytes(base64: string): Uint8Array {
        const binary = atob(base64)
        return Uint8Array.from(binary, char => char.charCodeAt(0))
    }

    function decodeBase64Text(base64: string, encoding: string = "utf-8"): string {
        const bytes = base64ToBytes(base64)
        return new TextDecoder(encoding).decode(bytes)
    }
    
    const queryClient = useQueryClient()

    const content = decodeBase64Text(fileQuery.data?.content ?? "", "utf-8")

    const [updatedContent, setUpdatedContent] = useState<string | null>(null)

    const editorContent = updatedContent ?? content
    
    const [saving, setSaving] = useState(false)

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["instance", id, "file", directory, name],
        })
    }
    
    const save = async () => {
        setSaving(true)
        try {
            const file = new File([new TextEncoder().encode(editorContent)], name, {type: "application/octet-stream"})
            await uploadFile(id, "./" + [...directory, name].join("/"), file)
            toast.add({
                type: "success",
                title: `Save successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to save.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setSaving(false)
        }
    }
    
    return (
        <div className={className}>
            <Card>
                <CardHeader>
                    <div className="flex flex-row items-center">
                        <div className="flex flex-row gap-2">
                            <PiFile className="min-w-5 min-h-5 text-primary" />
                            <div className="text">{[...directory, name].join("/")}</div>
                        </div>
                        <div className="ml-auto flex flex-row gap-2">
                            <Button
                                variant="ghost"
                                size="icon-sm"
                                className="hover:text-primary"
                                onClick={save}
                                disabled={saving}
                            >
                                <PiFloppyDisk />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon-sm"
                                className="hover:text-destructive"
                                onClick={closeCallback}
                            >
                                <PiX />
                            </Button>
                        </div>
                    </div>
                    <div className="flex flex-row items-center">
                        
                    </div>
                </CardHeader>
                <CardContent>
                    <Textarea
                        value={editorContent}
                        onChange={(e) => setUpdatedContent(e.target.value)}
                        className="min-h-[300px] max-h-[1000px] w-full resize-y font-mono text-sm leading-6 scrollbar-none [&::-webkit-scrollbar]:hidden"
                    />
                </CardContent>
            </Card>
        </div>
    )
}