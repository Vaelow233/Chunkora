import type {InstanceDto} from "@/types/instance.ts";
import {Card, CardContent} from "@/components/ui/card.tsx";
import {
    Dialog, DialogClose,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    PiArrowCounterClockwise, PiCalendar, PiCaretLeft, PiCaretRight,
    PiDotsThreeBold, PiNotePencil, PiPlus, PiTrash
} from "react-icons/pi";
import {Field, FieldDescription, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table.tsx";
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {createInstanceTask, deleteInstanceTask, listInstanceTasks, updateInstanceTask} from "@/api/task.ts";
import {useEffect, useState} from "react";
import { Popover, PopoverContent, PopoverTrigger } from "../ui/popover";
import { Calendar } from "../ui/calendar";
import { Select, SelectGroup, SelectContent, SelectTrigger, SelectValue, SelectItem } from "../ui/select";
import {toast} from "@/components/ui/toast.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import type {InstanceTaskDto} from "@/types/task.ts";

interface InstanceTaskManagerProps {
    id: string
    instance: InstanceDto | undefined
}

const triggerItems = [
    { label: "", value: 0 },
    { label: "Once", value: 1 },
    { label: "Interval", value: 2 },
    { label: "Cron (Scheduled)", value: 3 },
    { label: "On Agent Launch", value: 4 },
]

const actionItems = [
    { label: "", value: 0 },
    { label: "Stop", value: 1 },
    { label: "Start", value: 2 },
    { label: "Kill", value: 3 },
    { label: "Restart", value: 4 },
    { label: "Send Command", value: 5 },
]

interface InstanceTaskTriggerBadgeProps {
    trigger: number
    triggerInfo: string
}

function InstanceTaskTriggerBadge({trigger, triggerInfo}: InstanceTaskTriggerBadgeProps) {
    if (trigger === 1) {
        return <Badge variant="outline">{triggerInfo}</Badge>
    } else if (trigger === 2) {
        return <Badge variant="outline">Every {triggerInfo} second(s)</Badge>
    } else if (trigger === 3) {
        return <Badge variant="outline">Cron: {triggerInfo}</Badge>
    } else if (trigger === 4) {
        return <Badge variant="outline">On agent launch</Badge>
    } else {
        return <></>
    }
}

interface InstanceTaskActionBadgeProps {
    action: number
    actionInfo: string
}

function InstanceTaskActionBadge({action, actionInfo}: InstanceTaskActionBadgeProps) {
    if (action === 1) {
        return <Badge className="bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300">Stop</Badge>
    } else if (action === 2) {
        return <Badge className="bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-300">Start</Badge>
    } else if (action === 3) {
        return <Badge className="bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300">Kill</Badge>
    } else if (action === 4) {
        return <Badge className="bg-purple-50 text-purple-700 dark:bg-purple-950 dark:text-purple-300">Restart</Badge>
    } else if (action === 5) {
        return <Badge className="bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-300">Send: {actionInfo}</Badge>
    } else {
        return <></>
    }
}

export function InstanceTaskManager({id, instance}: InstanceTaskManagerProps) {
    const queryClient = useQueryClient()
    
    const PAGE_SIZE = 20

    const [page, setPage] = useState(1)
    
    const tasksQuery = useQuery({
        queryKey: ["instance", id, "tasks", page],
        queryFn: () => listInstanceTasks(id, (page - 1) * PAGE_SIZE, PAGE_SIZE),
    })

    const totalPage = Math.ceil((tasksQuery.data?.total ?? 0) / PAGE_SIZE)
    
    const [openTaskCreation, setOpenTaskCreation] = useState(false)
    const [name, setName] = useState("")
    const [description, setDescription] = useState("")
    const [trigger, setTrigger] = useState(0)
    const [triggerInfo, setTriggerInfo] = useState("")
    const [date, setDate] = useState<Date | undefined>(undefined) // when creating, date will be converted to triggerInfo
    const [time, setTime] = useState("00:00:00"); // same as date
    const [action, setAction] = useState(0)
    const [actionInfo, setActionInfo] = useState("")
    const [creatingTask, setCreatingTask] = useState(false)
    
    const [deleting, setDeleting] = useState(false)

    const [updatingId, setUpdatingId] = useState("")
    const [updatingNodeId, setUpdatingNodeId] = useState("")
    const [updatingInstanceId, setUpdatingInstanceId] = useState("")
    const [openEditing, setOpenEditing] = useState(false)
    const [updating, setUpdating] = useState(false)
    const [updatedName, setUpdatedName] = useState("")
    const [updatedDescription, setUpdatedDescription] = useState("")
    const [updatedTrigger, setUpdatedTrigger] = useState(0)
    const [updatedTriggerInfo, setUpdatedTriggerInfo] = useState("")
    const [updatedDate, setUpdatedDate] = useState<Date | undefined>(undefined) // when updating, date will be converted to triggerInfo
    const [updatedTime, setUpdatedTime] = useState("00:00:00"); // same as date
    const [updatedAction, setUpdatedAction] = useState(0)
    const [updatedActionInfo, setUpdatedActionInfo] = useState("")

    useEffect(() => {
        if (!tasksQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch instance tasks.",
            description: tasksQuery.error.message,
        })
    }, [tasksQuery.error])
    
    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["instance", id, "tasks"],
        })
    }

    const setUpdatingInfo = (task: InstanceTaskDto) => {
        setUpdatingId(task.id)
        setUpdatingNodeId(task.nodeId)
        setUpdatingInstanceId(task.instanceId)
        setUpdatedName(task.name)
        setUpdatedDescription(task.description)
        setUpdatedTrigger(task.trigger)
        setUpdatedTriggerInfo(task.triggerInfo)
        const parsedDate = task.trigger === 1 ? new Date(task.triggerInfo) : undefined
        const validDate = parsedDate && !Number.isNaN(parsedDate.getTime()) ? parsedDate : undefined
        setUpdatedDate(validDate)
        setUpdatedTime(validDate ? [validDate.getHours(), validDate.getMinutes(), validDate.getSeconds()]
                .map(v => String(v).padStart(2, "0")).join(":") : "00:00:00")
        setUpdatedAction(task.action)
        setUpdatedActionInfo(task.actionInfo)
    }

    const update = async () => {
        let nextTriggerInfo = updatedTriggerInfo;

        if (updatedTrigger === 1 && updatedDate !== undefined) {
            const [hours, minutes, seconds] = (updatedTime || "00:00:00")
                .split(":")
                .map(Number);
            const result = new Date(updatedDate);
            result.setHours(hours, minutes, seconds, 0);
            nextTriggerInfo = result.toISOString()
        }
        if (!updatedName || updatedTrigger === 0 || (updatedTrigger !== 4 && !nextTriggerInfo) || updatedAction === 0 
            || (updatedAction === 5 && !updatedActionInfo)) {
            toast.add({
                type: "error",
                title: "Failed to update task.",
                description: "All of the parameters except for the description should be non-empty",
            })
            return
        }
        setUpdating(true)
        try {
            await updateInstanceTask({
                id: updatingId,
                nodeId: updatingNodeId,
                instanceId: updatingInstanceId,
                name: updatedName,
                description: updatedDescription,
                trigger: updatedTrigger,
                triggerInfo: nextTriggerInfo,
                action: updatedAction,
                actionInfo: updatedActionInfo,
            })
            toast.add({
                type: "success",
                title: `Update task ${updatedName} successfully.`,
            })
            setOpenEditing(false)
            setUpdatingId("")
            setUpdatingNodeId("")
            setUpdatingInstanceId("")
            setUpdatedName("")
            setUpdatedDescription("")
            setUpdatedTrigger(0)
            setUpdatedTriggerInfo("")
            setUpdatedAction(0)
            setUpdatedActionInfo("")
            setUpdatedDate(undefined)
            setUpdatedTime("00:00:00")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to update task.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUpdating(false)
        }
    }
    
    const createTask = async () => {
        let nextTriggerInfo = triggerInfo;
        
        if (trigger === 1 && date !== undefined) {
            const [hours, minutes, seconds] = (time || "00:00:00")
                .split(":")
                .map(Number);
            const result = new Date(date);
            result.setHours(hours, minutes, seconds, 0);
            nextTriggerInfo = result.toISOString()
        }
        if (!name || trigger === 0 || (trigger !== 4 && !nextTriggerInfo) || action === 0 || (action === 5 && !actionInfo)) {
            toast.add({
                type: "error",
                title: "Failed to create task.",
                description: "All of the parameters except for the description should be non-empty",
            })
            return
        }
        setCreatingTask(true)
        try {
            await createInstanceTask({
                nodeId: instance!.nodeId,
                instanceId: instance!.id,
                name,
                description,
                trigger,
                triggerInfo: nextTriggerInfo,
                action,
                actionInfo,
            })
            toast.add({
                type: "success",
                title: `Create task ${name} successfully.`,
            })
            setOpenTaskCreation(false)
            setName("")
            setDescription("")
            setTrigger(0)
            setTriggerInfo("")
            setAction(0)
            setActionInfo("")
            setDate(undefined)
            setTime("00:00:00")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create task.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreatingTask(false)
        }
    }
    
    const del = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to delete task.",
                description: "The id of the task should be non-empty.",
            })
            return
        }
        setDeleting(true)
        try {
            await deleteInstanceTask(instance!.id, id)
            toast.add({
                type: "success",
                title: `Delete task ${id} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete task.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setDeleting(false)
        }
    }
    
    return (
        <div>
            <Card>
                <CardContent>
                    <div className="flex flex-row gap-4">
                        <Dialog open={openTaskCreation} onOpenChange={setOpenTaskCreation}>
                            <DialogTrigger render={
                                <Button className="w-32">
                                    <PiPlus className="min-w-5 min-h-5" />
                                    New Task
                                </Button>
                            } />
                            <DialogContent>
                                <DialogHeader>
                                    <DialogTitle>Create New Task</DialogTitle>
                                </DialogHeader>
                                <FieldGroup>
                                    <Field>
                                        <FieldLabel>Name</FieldLabel>
                                        <Input value={name}
                                               onChange={(event) => setName(event.target.value)}
                                               type="text" />
                                    </Field>
                                    <Field>
                                        <FieldLabel>Description</FieldLabel>
                                        <Input value={description}
                                               onChange={(event) => setDescription(event.target.value)}
                                               type="text" />
                                    </Field>
                                    <Field>
                                        <FieldLabel>Trigger</FieldLabel>
                                        <FieldDescription>Select the method to express the trigger time</FieldDescription>
                                        <Select
                                            value={trigger}
                                            onValueChange={(newValue) => setTrigger(newValue ?? 0)}
                                            items={triggerItems}
                                        >
                                            <SelectTrigger className="w-full max-w-48">
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectGroup>
                                                    {
                                                        triggerItems.map((item) => (
                                                            item.value === 0 ? <></> : 
                                                                <SelectItem key={item.value} value={item.value}>
                                                                    {item.label}
                                                                </SelectItem>
                                                        ))
                                                    }
                                                </SelectGroup>
                                            </SelectContent>
                                        </Select>
                                    </Field>
                                    {
                                        trigger === 4 ? <></> : (
                                            <Field>
                                                <FieldLabel>Trigger Information</FieldLabel>
                                                <FieldDescription>
                                                    {
                                                        trigger === 1 ? "Select the date and time of the trigger" : 
                                                            (trigger === 2 ? "Input the interval time (in seconds) of the trigger" : 
                                                                (trigger === 3 ?  "Input the cron expression as the trigger time" : "")
                                                            )
                                                    }
                                                </FieldDescription>
                                                {
                                                    trigger === 1 ? (
                                                        <div className="flex flex-row gap-4">
                                                            <Popover>
                                                                <PopoverTrigger
                                                                    render={
                                                                        <Button
                                                                            variant="outline"
                                                                            data-empty={!date}
                                                                            className="flex-1 justify-start text-left font-normal data-[empty=true]:text-muted-foreground"
                                                                        />
                                                                    }
                                                                >
                                                                    <PiCalendar />
                                                                    {date ? date.toDateString() : <span>Pick a date to trigger</span>}
                                                                </PopoverTrigger>
                                                                <PopoverContent className="w-auto p-0">
                                                                    <Calendar mode="single" selected={date} onSelect={setDate} />
                                                                </PopoverContent>
                                                            </Popover>
                                                            <Input
                                                                type="time"
                                                                id="time-picker-optional"
                                                                step="1"
                                                                value={time}
                                                                onChange={(event) => setTime(event.target.value)}
                                                                className="flex-1 appearance-none [&::-webkit-calendar-picker-indicator]:hidden [&::-webkit-calendar-picker-indicator]:appearance-none"
                                                            />
                                                        </div>
                                                    ) : (
                                                        <>
                                                            <Input value={triggerInfo}
                                                                   onChange={(event) => setTriggerInfo(event.target.value)}
                                                                   type="text" />
                                                        </>
                                                    )
                                                }
                                            </Field>
                                        )
                                    }
                                    <Field>
                                        <FieldLabel>Action</FieldLabel>
                                        <FieldDescription>Select the action to do when trigger</FieldDescription>
                                        <Select
                                            value={action}
                                            onValueChange={(newValue) => setAction(newValue ?? 0)}
                                            items={actionItems}
                                        >
                                            <SelectTrigger className="w-full max-w-48">
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectGroup>
                                                    {
                                                        actionItems.map((item) => (
                                                            item.value === 0 ? <></> :
                                                                <SelectItem key={item.value} value={item.value}>
                                                                    {item.label}
                                                                </SelectItem>
                                                        ))
                                                    }
                                                </SelectGroup>
                                            </SelectContent>
                                        </Select>
                                    </Field>
                                    {
                                        action !== 5 ? <></> : (
                                            <Field>
                                                <FieldLabel>Action Information</FieldLabel>
                                                <FieldDescription>
                                                    {
                                                        action === 5 ? "Input the command to send to the instance stdin" : ""
                                                    }
                                                </FieldDescription>
                                                <Input value={actionInfo}
                                                       onChange={(event) => setActionInfo(event.target.value)}
                                                       type="text" />
                                            </Field>
                                        )
                                    }
                                </FieldGroup>
                                <DialogFooter>
                                    <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                    <Button type="submit" onClick={createTask} disabled={creatingTask}>Create</Button>
                                </DialogFooter>
                            </DialogContent>
                        </Dialog>
                        <Button variant="outline" className="w-32" onClick={refresh}>
                            <PiArrowCounterClockwise className="min-w-5 min-h-5" />
                            Refresh
                        </Button>
                    </div>
                </CardContent>
            </Card>
            <div className="mt-4 grid min-w-0 gap-4 grid-cols-1">
                <Table className="min-w-0">
                    <TableHeader>
                        <TableRow>
                            <TableHead>Name</TableHead>
                            <TableHead>Trigger</TableHead>
                            <TableHead>Action</TableHead>
                            <TableHead>Last Ran</TableHead>
                            <TableHead>Next Run</TableHead>
                            <TableHead>Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {
                            tasksQuery.data?.items.map((task) => (
                                <TableRow key={task.id}>
                                    <TableCell>
                                        {task.name}
                                    </TableCell>
                                    <TableCell>
                                        <InstanceTaskTriggerBadge trigger={task.trigger} triggerInfo={task.triggerInfo} />
                                    </TableCell>
                                    <TableCell>
                                        <InstanceTaskActionBadge action={task.action} actionInfo={task.actionInfo} />
                                    </TableCell>
                                    <TableCell>
                                        {
                                            task.lastRunAt ? new Date(task.lastRunAt).toLocaleString() : ""
                                        }
                                    </TableCell>
                                    <TableCell>
                                        {
                                            task.nextRunAt ? new Date(task.nextRunAt).toLocaleString() : ""
                                        }
                                    </TableCell>
                                    <TableCell>
                                        <Dialog>
                                            <DialogTrigger render={
                                                <Button variant="outline" className="w-32 mr-6">
                                                    <PiDotsThreeBold />
                                                    Detail
                                                </Button>
                                            } />
                                            <DialogContent>
                                                <DialogHeader>
                                                    <DialogTitle>Task Detail</DialogTitle>
                                                </DialogHeader>
                                                <FieldGroup>
                                                    <Field>
                                                        <FieldLabel>Name</FieldLabel>
                                                        <div>{task.name}</div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>ID</FieldLabel>
                                                        <div>{task.id}</div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Node ID</FieldLabel>
                                                        <div>{task.nodeId}</div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Instance ID</FieldLabel>
                                                        <div>{task.instanceId}</div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Description</FieldLabel>
                                                        <div>{task.description}</div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Trigger</FieldLabel>
                                                        <div className="mr-auto">
                                                            <InstanceTaskTriggerBadge trigger={task.trigger} triggerInfo={task.triggerInfo} />
                                                        </div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Action</FieldLabel>
                                                        <div className="mr-auto">
                                                            <InstanceTaskActionBadge action={task.action} actionInfo={task.actionInfo} />
                                                        </div>
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Last Ran</FieldLabel>
                                                        {
                                                            task.lastRunAt ? new Date(task.lastRunAt).toLocaleString() : ""
                                                        }
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Next Run</FieldLabel>
                                                        {
                                                            task.nextRunAt ? new Date(task.nextRunAt).toLocaleString() : ""
                                                        }
                                                    </Field>
                                                </FieldGroup>
                                            </DialogContent>
                                        </Dialog>
                                        <Dialog open={openEditing && updatingId === task.id} onOpenChange={setOpenEditing}>
                                            <DialogTrigger render={
                                                <Button className="w-32 mr-6" variant="outline" onClick={() => setUpdatingInfo(task)}>
                                                    <PiNotePencil />
                                                    Edit
                                                </Button>
                                            } />
                                            <DialogContent>
                                                <DialogHeader>
                                                    <DialogTitle>Edit Task</DialogTitle>
                                                </DialogHeader>
                                                <FieldGroup>
                                                    <Field>
                                                        <FieldLabel>Name</FieldLabel>
                                                        <Input value={updatedName}
                                                               onChange={(event) => setUpdatedName(event.target.value)}
                                                               type="text" />
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Description</FieldLabel>
                                                        <Input value={updatedDescription}
                                                               onChange={(event) => setUpdatedDescription(event.target.value)}
                                                               type="text" />
                                                    </Field>
                                                    <Field>
                                                        <FieldLabel>Trigger</FieldLabel>
                                                        <FieldDescription>Select the method to express the trigger time</FieldDescription>
                                                        <Select
                                                            value={updatedTrigger}
                                                            onValueChange={(newValue) => setUpdatedTrigger(newValue ?? 0)}
                                                            items={triggerItems}
                                                        >
                                                            <SelectTrigger className="w-full max-w-48">
                                                                <SelectValue />
                                                            </SelectTrigger>
                                                            <SelectContent>
                                                                <SelectGroup>
                                                                    {
                                                                        triggerItems.map((item) => (
                                                                            item.value === 0 ? <></> :
                                                                                <SelectItem key={item.value} value={item.value}>
                                                                                    {item.label}
                                                                                </SelectItem>
                                                                        ))
                                                                    }
                                                                </SelectGroup>
                                                            </SelectContent>
                                                        </Select>
                                                    </Field>
                                                    {
                                                        updatedTrigger === 4 ? <></> : (
                                                            <Field>
                                                                <FieldLabel>Trigger Information</FieldLabel>
                                                                <FieldDescription>
                                                                    {
                                                                        updatedTrigger === 1 ? "Select the date and time of the trigger" :
                                                                            (updatedTrigger === 2 ? "Input the interval time (in seconds) of the trigger" :
                                                                                    (updatedTrigger === 3 ?  "Input the cron expression as the trigger time" : "")
                                                                            )
                                                                    }
                                                                </FieldDescription>
                                                                {
                                                                    updatedTrigger === 1 ? (
                                                                        <div className="flex flex-row gap-4">
                                                                            <Popover>
                                                                                <PopoverTrigger
                                                                                    render={
                                                                                        <Button
                                                                                            variant="outline"
                                                                                            data-empty={!updatedDate}
                                                                                            className="flex-1 justify-start text-left font-normal data-[empty=true]:text-muted-foreground"
                                                                                        />
                                                                                    }
                                                                                >
                                                                                    <PiCalendar />
                                                                                    {updatedDate ? updatedDate.toDateString() : <span>Pick a date to trigger</span>}
                                                                                </PopoverTrigger>
                                                                                <PopoverContent className="w-auto p-0">
                                                                                    <Calendar mode="single" selected={updatedDate} onSelect={setUpdatedDate} />
                                                                                </PopoverContent>
                                                                            </Popover>
                                                                            <Input
                                                                                type="time"
                                                                                id="time-picker-optional"
                                                                                step="1"
                                                                                value={updatedTime}
                                                                                onChange={(event) => setUpdatedTime(event.target.value)}
                                                                                className="flex-1 appearance-none [&::-webkit-calendar-picker-indicator]:hidden [&::-webkit-calendar-picker-indicator]:appearance-none"
                                                                            />
                                                                        </div>
                                                                    ) : (
                                                                        <>
                                                                            <Input value={updatedTriggerInfo}
                                                                                   onChange={(event) => setUpdatedTriggerInfo(event.target.value)}
                                                                                   type="text" />
                                                                        </>
                                                                    )
                                                                }
                                                            </Field>
                                                        )
                                                    }
                                                    <Field>
                                                        <FieldLabel>Action</FieldLabel>
                                                        <FieldDescription>Select the action to do when trigger</FieldDescription>
                                                        <Select
                                                            value={updatedAction}
                                                            onValueChange={(newValue) => setUpdatedAction(newValue ?? 0)}
                                                            items={actionItems}
                                                        >
                                                            <SelectTrigger className="w-full max-w-48">
                                                                <SelectValue />
                                                            </SelectTrigger>
                                                            <SelectContent>
                                                                <SelectGroup>
                                                                    {
                                                                        actionItems.map((item) => (
                                                                            item.value === 0 ? <></> :
                                                                                <SelectItem key={item.value} value={item.value}>
                                                                                    {item.label}
                                                                                </SelectItem>
                                                                        ))
                                                                    }
                                                                </SelectGroup>
                                                            </SelectContent>
                                                        </Select>
                                                    </Field>
                                                    {
                                                        updatedAction !== 5 ? <></> : (
                                                            <Field>
                                                                <FieldLabel>Action Information</FieldLabel>
                                                                <FieldDescription>
                                                                    {
                                                                        updatedAction === 5 ? "Input the command to send to the instance stdin" : ""
                                                                    }
                                                                </FieldDescription>
                                                                <Input value={updatedActionInfo}
                                                                       onChange={(event) => setUpdatedActionInfo(event.target.value)}
                                                                       type="text" />
                                                            </Field>
                                                        )
                                                    }
                                                </FieldGroup>
                                                <DialogFooter>
                                                    <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                                    <Button type="submit" onClick={update} disabled={updating}>Update</Button>
                                                </DialogFooter>
                                            </DialogContent>
                                        </Dialog>
                                        <Button className="w-32" variant="destructive" onClick={() => del(task.id)} disabled={deleting}>
                                            <PiTrash />
                                            Delete
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))
                        }
                    </TableBody>
                </Table>
            </div>
            <div className="flex flex-row mt-6 gap-3 float-right items-center">
                <Button variant="outline" size="icon-sm" onClick={() => setPage(page - 1)} disabled={page <= 1}>
                    <PiCaretLeft />
                </Button>
                <div>{page} / {totalPage}</div>
                <Button variant="outline" size="icon-sm" onClick={() => setPage(page + 1)} disabled={page >= totalPage}>
                    <PiCaretRight />
                </Button>
            </div>
        </div>
    )
}