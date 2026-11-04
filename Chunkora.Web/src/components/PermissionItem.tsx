import {Collapsible, CollapsibleContent, CollapsibleTrigger} from "@/components/ui/collapsible.tsx";
import {PiCaretDownBold, PiCaretRightBold} from "react-icons/pi";
import { Checkbox } from "./ui/checkbox";
import {useState} from "react";
import type {PermissionNode} from "@/utils/permission.ts";

export interface PermissionItemProps {
    node: PermissionNode
    checkInclude: (permission: string) => boolean
    updateStatus: (permission: string, checked: boolean) => void
}

export function PermissionItem({ node, checkInclude, updateStatus }: PermissionItemProps) {
    const [open, setOpen] = useState(false)
    
    return (
        <Collapsible className="w-full" open={open} onOpenChange={setOpen}>
            <div className="flex items-center gap-2">
                <CollapsibleTrigger>
                    {
                        node.children ? (open ? <PiCaretDownBold /> : <PiCaretRightBold />) : (<div className="w-4 h-4" />)
                    }
                </CollapsibleTrigger>
                <Checkbox
                    checked={checkInclude(node.permission ?? "")}
                    onCheckedChange={(checked) => updateStatus(node.permission ?? "", checked)}
                />
                <span>{node.name}</span>
                <span className="text-xs text-muted-foreground ml-auto">{node.description}</span>
            </div>
            {node.children && (
                <CollapsibleContent className="ml-6">
                    {
                        node.children.map(child => (
                            <PermissionItem 
                                key={child.name}
                                node={child}
                                checkInclude={checkInclude}
                               updateStatus={updateStatus}
                            />
                        ))
                    }
                </CollapsibleContent>
            )}
        </Collapsible>
    )
}