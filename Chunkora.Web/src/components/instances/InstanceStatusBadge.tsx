import {Badge} from "@/components/ui/badge.tsx";

interface InstanceStatusBadgeProps {
    status: number
}

export function InstanceStatusBadge({status}: InstanceStatusBadgeProps) {
    switch (status) {
        case 1:
            return (<Badge className="bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-300">Starting</Badge>)
        case 2:
            return (<Badge className="bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-300">Running</Badge>)
        case 3:
            return (<Badge className="bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300">Stopping</Badge>)
        case 4:
            return (<Badge className="bg-purple-50 text-purple-700 dark:bg-purple-950 dark:text-purple-300">Restarting</Badge>)
        default:
            return (<Badge variant="outline">Stopped</Badge>)
    }
}