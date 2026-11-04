import * as React from "react";

interface InfoFieldProps {
    icon: React.ReactNode
    label: string
    children: React.ReactNode
}

export function InfoField({icon, label, children}: InfoFieldProps) {
    return (
        <div className="flex flex-row gap-2 items-center bg-muted rounded-lg px-2 py-1 border-border ring-1 ring-foreground/10">
            {icon}
            <div className="text-muted-foreground">{label}: </div>
            <div className="text-base">{children}</div>
        </div>
    )
}