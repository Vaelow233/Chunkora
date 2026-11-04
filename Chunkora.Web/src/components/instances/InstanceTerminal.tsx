import {
    CodeBlock,
    CodeBlockContent,
    CodeBlockHeader,
    CodeBlockLine,
    CodeBlockTitle
} from "@/components/ui/code-block.tsx";
import {useQuery} from "@tanstack/react-query";
import {getInstanceLogs} from "@/api/instance.ts";
import {useEffect, useRef} from "react";
import {toast} from "@/components/ui/toast.tsx";

interface InstanceTerminalProps {
    id: string
    className?: string
}

export function InstanceTerminal({id, className}: InstanceTerminalProps) {
    const scrollRef = useRef<HTMLPreElement>(null)
    
    const instanceLogsQuery = useQuery({
        queryKey: ["instance", "logs", id],
        queryFn: () => getInstanceLogs(id, 0, 0),
        refetchInterval: 1000,
    })

    useEffect(() => {
        if (!instanceLogsQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to fetch the logs of instance ${id}.`,
            description: instanceLogsQuery.error.message,
        })
    }, [instanceLogsQuery.error])

    let nextLimit = 2000, nextOffset = -1
    if (instanceLogsQuery.data) {
        if (instanceLogsQuery.data.total > 2000) {
            nextOffset = instanceLogsQuery.data.total - 2000
        } else {
            nextOffset = 0
        }
    }
    
    const instanceShownLogsQuery = useQuery({
        queryKey: ["instance", "logs", id, `limit-${nextLimit}`, `offset-${nextOffset}`],
        queryFn: () => getInstanceLogs(id, nextOffset, nextLimit),
        enabled: nextOffset >= 0,
        refetchInterval: 1000
    })

    const lines = instanceShownLogsQuery.data?.items

    useEffect(() => {
        if (!instanceShownLogsQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to fetch the logs of instance ${id}.`,
            description: instanceShownLogsQuery.error.message,
        })
    }, [instanceShownLogsQuery.error])

    useEffect(() => {
        const element = scrollRef.current
        if (!element) return

        element.scrollTop = element.scrollHeight
    }, [lines])
    
    return (
        <CodeBlock className={className}>
            <CodeBlockHeader>
                <CodeBlockTitle>
                    Terminal
                </CodeBlockTitle>
            </CodeBlockHeader>
            <CodeBlockContent className="scrollbar-none [&::-webkit-scrollbar]:hidden" ref={scrollRef} wrap>
                {
                    lines?.map((line, index) => (
                        <CodeBlockLine key={index}>{line}</CodeBlockLine>
                    ))
                }
            </CodeBlockContent>
        </CodeBlock>
    )
}