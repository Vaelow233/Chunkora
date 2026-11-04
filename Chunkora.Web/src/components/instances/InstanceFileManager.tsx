import {Card, CardContent} from "@/components/ui/card.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    PiArrowCounterClockwise, PiCaretLeft, PiCaretRight, PiClipboard, PiCopy, PiDownload,
    PiFile,
    PiFileArrowUp,
    PiFilePlus,
    PiFolder,
    PiFolderPlus, PiNotePencil, PiPencil, PiScissors,
    PiTrash
} from "react-icons/pi";
import {useEffect, useRef, useState} from "react";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbSeparator
} from "@/components/ui/breadcrumb.tsx";
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {toast} from "@/components/ui/toast.tsx";
import {
    copyDirectory,
    copyFile,
    createNewDirectory,
    createNewFile,
    deleteDirectory,
    deleteFile,
    downloadFile,
    listFiles, moveDirectory, moveFile, renameDirectory, renameFile,
    uploadFile
} from "@/api/instance.ts";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table.tsx";
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger
} from "@/components/ui/dialog.tsx";
import {Field, FieldGroup, FieldLabel} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {InstanceFileEditor} from "@/components/instances/InstanceFileEditor.tsx";
import {useFileManagerStore} from "@/stores/file-manager.ts";

interface InstanceFileManagerProps {
    id: string
}

function formatByte(bytes: number): string {
    if (bytes < 1024) {
        return `${bytes} B`;
    } else if (bytes < 1024 * 1024) {
        return `${(bytes / 1024).toFixed(2)} KB`;
    } else if (bytes < 1024 * 1024 * 1024) {
        return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
    } else if (bytes < 1024 * 1024 * 1024 * 1024) {
        return `${(bytes / 1024 / 1024 / 1024).toFixed(2)} GB`;
    } else {
        return `${(bytes / 1024 / 1024 / 1024 / 1024).toFixed(2)} TB`;
    }
}

export function InstanceFileManager({id}: InstanceFileManagerProps) {
    const queryClient = useQueryClient()
    
    const PAGE_SIZE = 20

    const getDirectory = useFileManagerStore(state => state.get)
    const updateDirectory = useFileManagerStore(state => state.update)
    
    const [directory, setDirectory] = useState<string[]>(() => getDirectory(id)?.directories ?? [])
    const [page, setPage] = useState(1)
    
    const [fileToCreate, setFileToCreate] = useState("")
    const [openFileCreation, setOpenFileCreation] = useState(false)
    const [creatingFile, setCreatingFile] = useState(false)
    const [dirToCreate, setDirToCreate] = useState("")
    const [openDirCreation, setOpenDirCreation] = useState(false)
    const [creatingDir, setCreatingDir] = useState(false)
    const inputRef = useRef<HTMLInputElement>(null)
    const [uploading, setUploading] = useState(false)
    const [openRename, setOpenRename] = useState(false)
    const [originName, setOriginName] = useState("")
    const [originType, setOriginType] = useState("")
    const [newName, setNewName] = useState("")
    const [renaming, setRenaming] = useState(false)
    
    const [pathToCopy, setPathToCopy] = useState("")
    const [fileToCopy, setFileToCopy] = useState("")
    const [pathToMove, setPathToMove] = useState("")
    const [fileToMove, setFileToMove] = useState("")
    const [isFileCopyOrMove, setIsFileCopyOrMove] = useState(false)

    const [editing, setEditing] = useState(false)
    const [editingDirectory, setEditingDirectory] = useState<string[]>([])
    const [editingName, setEditingName] = useState("")

    const filesQuery = useQuery({
        queryKey: ["instance", id, "files", directory, page],
        queryFn: () => listFiles(id, "./" + directory.join("/"), (page - 1) * PAGE_SIZE, PAGE_SIZE),
    })

    const totalPage = Math.ceil((filesQuery.data?.total ?? 0) / PAGE_SIZE)

    useEffect(() => {
        updateDirectory(id, directory)
    }, [id, directory, updateDirectory])

    useEffect(() => {
        if (!filesQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: "Failed to fetch instance files.",
            description: filesQuery.error.message,
        })
    }, [filesQuery.error])
    
    const handleClick = async (type: string, name: string, size: number) => {
        if (type == "directory") {
            setDirectory(directory.concat(name))
            setPage(1)
        } else if (type == "file") {
            if (size > 1024 * 1024 * 20) {
                await download(name)
            } else {
                edit(name)
            }
        }
    }
    
    const download = async (name: string) => {
        try {
            await downloadFile(id, "./" + [...directory, name].join("/"))
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to download file",
                description: (e instanceof Error ? e.message : String(e)),
            })
        }
    }

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["instance", id, "files", directory, page],
        })
    }
    
    const createFile = async () => {
        if (!fileToCreate) {
            toast.add({
                type: "error",
                title: "Failed to create file.",
                description: "The name of the file should be non-empty.",
            })
            return
        }
        setCreatingFile(true)
        try {
            await createNewFile(id, "./" + directory.join("/"), fileToCreate)
            toast.add({
                type: "success",
                title: `Create file ${fileToCreate} successfully.`,
            })
            setOpenFileCreation(false)
            setFileToCreate("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create file.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreatingFile(false)
        }
    }

    const createDir = async () => {
        if (!dirToCreate) {
            toast.add({
                type: "error",
                title: "Failed to create folder.",
                description: "The name of the folder should be non-empty.",
            })
            return
        }
        setCreatingDir(true)
        try {
            await createNewDirectory(id, "./" + directory.join("/"), dirToCreate)
            toast.add({
                type: "success",
                title: `Create folder ${dirToCreate} successfully.`,
            })
            setOpenDirCreation(false)
            setDirToCreate("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to create folder.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setCreatingDir(false)
        }
    }

    const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0]
        event.target.value = ""
        if (!file) {
            toast.add({
                type: "error",
                title: "Failed to upload file.",
                description: "The file should not be null.",
            })
            return
        }
        if (uploading) {
            toast.add({
                type: "error",
                title: "Failed to upload file.",
                description: "There is a file that is already uploading.",
            })
            return
        }
        setUploading(true)
        try {
            await uploadFile(id, "./" + [...directory, file.name].join("/"), file)
            toast.add({
                type: "success",
                title: `Upload file ${file.name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to upload file.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setUploading(false)
        }
    }

    const deleteObject = async (name: string, type: string) => {
        if (!name) {
            toast.add({
                type: "error",
                title: "Failed to delete object.",
                description: "The name of the object should be non-empty.",
            })
            return
        }
        try {
            if (type == "file") {
                await deleteFile(id, "./" + directory.join("/"), name)
            } else if (type == "directory") {
                await deleteDirectory(id, "./" + directory.join("/"), name)
            } else {
                toast.add({
                    type: "error",
                    title: "Failed to delete object.",
                    description: `Unknown object type ${type}.`,
                })
                return
            }
            toast.add({
                type: "success",
                title: `Delete object ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to delete object.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        }
    }
    
    const copy = (basePath: string, name: string, type: string) => {
        setPathToCopy(basePath)
        setFileToCopy(name)
        setPathToMove("")
        setFileToMove("")
        if (type === "file") {
            setIsFileCopyOrMove(true)
        } else {
            setIsFileCopyOrMove(false)
        }
        toast.add({
            type: "success",
            title: `Set object ${name} in the clipboard successfully.`,
            description: `Change to the target directory and press the "Paste" button above to complete copying.`,
        })
    }
    
    const move = (basePath: string, name: string, type: string) => {
        setPathToCopy("")
        setFileToCopy("")
        setPathToMove(basePath)
        setFileToMove(name)
        if (type === "file") {
            setIsFileCopyOrMove(true)
        } else {
            setIsFileCopyOrMove(false)
        }
        toast.add({
            type: "success",
            title: `Set object ${name} in the clipboard successfully.`,
            description: `Change to the target directory and press the "Paste" button above to complete moving.`,
        })
    }

    const paste = async () => {
        if (!fileToCopy && !fileToMove) {
            toast.add({
                type: "error",
                title: "Failed to paste object.",
                description: "The name of the object should be non-empty.",
            })
            return
        }
        try {
            if (fileToCopy) {
                if (isFileCopyOrMove) {
                    await copyFile(id, pathToCopy, fileToCopy, "./" + directory.join("/"))
                } else {
                    await copyDirectory(id, pathToCopy, fileToCopy, "./" + directory.join("/"))
                }
            } else if (fileToMove) {
                if (isFileCopyOrMove) {
                    await moveFile(id, pathToMove, fileToMove, "./" + directory.join("/"))
                } else {
                    await moveDirectory(id, pathToMove, fileToMove, "./" + directory.join("/"))
                }
            }
            toast.add({
                type: "success",
                title: `Paste object ${fileToCopy ?? fileToMove} successfully.`,
            })
            setPathToCopy("")
            setFileToCopy("")
            setPathToMove("")
            setFileToMove("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to paste object.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        }
    }

    const rename = async () => {
        if (!originName || !newName || originName === newName) {
            toast.add({
                type: "error",
                title: "Failed to rename object.",
                description: "The old name and new name of the object should be non-empty and different.",
            })
            return
        }
        setRenaming(true)
        try {
            if (originType == "file") {
                await renameFile(id, "./" + directory.join("/"), originName, newName)
            } else if (originType == "directory") {
                await renameDirectory(id, "./" + directory.join("/"), originName, newName)
            } else {
                toast.add({
                    type: "error",
                    title: "Failed to rename object.",
                    description: `Unknown object type ${originType}.`,
                })
                return
            }
            toast.add({
                type: "success",
                title: `Rename object ${originName} successfully.`,
            })
            setOpenRename(false)
            setOriginName("")
            setOriginType("")
            setNewName("")
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to rename object.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setRenaming(false)
        }
    }
    
    const openRenameDialog = (name: string, type: string) => {
        setOriginName(name)
        setOriginType(type)
        setNewName(name)
        setOpenRename(true)
    }
    
    const edit = (name: string) => {
        setEditing(true)
        setEditingDirectory(directory)
        setEditingName(name)
    }

    return (
        <div>
            <Card>
                <CardContent>
                    <div className="flex flex-row gap-4">
                        <Dialog open={openFileCreation} onOpenChange={setOpenFileCreation}>
                            <DialogTrigger render={
                                <Button className="w-32">
                                    <PiFilePlus className="min-w-5 min-h-5" />
                                    New File
                                </Button>
                            } />
                            <DialogContent>
                                <DialogHeader>
                                    <DialogTitle>Create New File</DialogTitle>
                                </DialogHeader>
                                <FieldGroup>
                                    <Field>
                                        <FieldLabel>Name</FieldLabel>
                                        <Input value={fileToCreate}
                                               onChange={(event) => setFileToCreate(event.target.value)}
                                               type="text" />
                                    </Field>
                                </FieldGroup>
                                <DialogFooter>
                                    <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                    <Button type="submit" onClick={createFile} disabled={creatingFile}>Create</Button>
                                </DialogFooter>
                            </DialogContent>
                        </Dialog>
                        <Dialog open={openDirCreation} onOpenChange={setOpenDirCreation}>
                            <DialogTrigger render={
                                <Button variant="outline" className="w-32">
                                    <PiFolderPlus className="min-w-5 min-h-5" />
                                    New Folder
                                </Button>
                            } />
                            <DialogContent>
                                <DialogHeader>
                                    <DialogTitle>Create New Folder</DialogTitle>
                                </DialogHeader>
                                <FieldGroup>
                                    <Field>
                                        <FieldLabel>Name</FieldLabel>
                                        <Input value={dirToCreate}
                                               onChange={(event) => setDirToCreate(event.target.value)}
                                               type="text" />
                                    </Field>
                                </FieldGroup>
                                <DialogFooter>
                                    <DialogClose render={<Button variant="outline">Cancel</Button>} />
                                    <Button type="submit" onClick={createDir} disabled={creatingDir}>Create</Button>
                                </DialogFooter>
                            </DialogContent>
                        </Dialog>
                        <input
                            ref={inputRef}
                            type="file"
                            className="hidden"
                            onChange={handleFileChange}
                            disabled={uploading}
                        />
                        <Button 
                            variant="outline" 
                            className="w-32"
                            onClick={() => inputRef.current?.click()}
                            disabled={uploading}
                        >
                            <PiFileArrowUp className="min-w-5 min-h-5" />
                            Upload
                        </Button>
                        <Button variant="outline" className="w-32" onClick={refresh}>
                            <PiArrowCounterClockwise className="min-w-5 min-h-5" />
                            Refresh
                        </Button>
                        {
                            !fileToCopy && !fileToMove ? <></> : (
                                <Button variant="outline" className="w-32" onClick={paste}>
                                    <PiClipboard className="min-w-5 min-h-5" />
                                    Paste
                                </Button>
                            )
                        }
                    </div>
                    <div className="flex flex-row mt-4">
                        <Breadcrumb>
                            <BreadcrumbList>
                                <BreadcrumbItem>
                                    <BreadcrumbLink render={
                                        <button type="button"
                                                className="underline-offset-4 hover:underline cursor-pointer text-base"
                                                onClick={() => {
                                                    setDirectory([])
                                                    setPage(1)
                                                }}
                                        >
                                            [ root ]
                                        </button>
                                    } />
                                </BreadcrumbItem>
                                <BreadcrumbSeparator>
                                    <div className="text-base">
                                        /
                                    </div>
                                </BreadcrumbSeparator>
                                {
                                    directory.map((item, index) => (
                                        <>
                                            <BreadcrumbItem>
                                                <BreadcrumbLink render={
                                                    <button type="button"
                                                            className="underline-offset-4 hover:underline cursor-pointer text-base"
                                                            onClick={() => {
                                                                setDirectory(prev => prev.slice(0, index + 1))
                                                                setPage(1)
                                                            }}
                                                    >
                                                        {item}
                                                    </button>
                                                } />
                                            </BreadcrumbItem>
                                            <BreadcrumbSeparator>
                                                <div className="text-base">
                                                    /
                                                </div>
                                            </BreadcrumbSeparator>
                                        </>
                                    ))
                                }
                            </BreadcrumbList>
                        </Breadcrumb>
                    </div>
                </CardContent>
            </Card>
            <div className={`mt-4 grid min-w-0 gap-4 ${editing ? "grid-cols-1 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]" : "grid-cols-1"}`}>
                <Table className="min-w-0">
                    <TableHeader>
                        <TableRow>
                            <TableHead>Name</TableHead>
                            <TableHead>Size</TableHead>
                            <TableHead>Permission</TableHead>
                            <TableHead>Created</TableHead>
                            <TableHead>Last Updated</TableHead>
                            <TableHead>Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {
                            filesQuery.data?.items.map((file) => (
                                <TableRow key={file.name}>
                                    <TableCell>
                                        <button type="button"
                                                className="underline-offset-4 hover:underline cursor-pointer"
                                                onClick={() => handleClick(file.type, file.name, file.sizeBytes)}
                                        >
                                            <div className="flex flex-row items-center gap-2 text-foreground">
                                                {
                                                    file.type == "file" ? <PiFile className="min-w-5 min-h-5" /> : <></>
                                                }
                                                {
                                                    file.type == "directory" ? <PiFolder className="min-w-5 min-h-5" /> : <></>
                                                }
                                                {file.name}
                                            </div>
                                        </button>
                                    </TableCell>
                                    <TableCell>
                                        {
                                            file.type == "file" ? formatByte(file.sizeBytes) : <></>
                                        }
                                    </TableCell>
                                    <TableCell>
                                        {file.permission}
                                    </TableCell>
                                    <TableCell>
                                        {
                                            new Date(file.createdAt).toLocaleString()
                                        }
                                    </TableCell>
                                    <TableCell>
                                        {
                                            new Date(file.lastUpdatedAt).toLocaleString()
                                        }
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex flex-row gap-2">
                                            {
                                                file.type !== "file" ? <></> : (
                                                    <>
                                                        {
                                                            file.sizeBytes > 1024 * 1024 * 20 ? <></> : (
                                                                <Button
                                                                    variant="outline"
                                                                    size="icon-sm"
                                                                    className="hover:text-primary"
                                                                    onClick={() => edit(file.name)}
                                                                >
                                                                    <PiPencil />
                                                                </Button>
                                                            )
                                                        }
                                                        <Button
                                                            variant="outline"
                                                            size="icon-sm"
                                                            className="hover:text-primary"
                                                            onClick={() => download(file.name)}
                                                        >
                                                            <PiDownload />
                                                        </Button>
                                                    </>
                                                )
                                            }
                                            <Button
                                                variant="outline"
                                                size="icon-sm"
                                                className="hover:text-primary"
                                                onClick={() => copy("./" + directory.join("/"), file.name, file.type)}
                                            >
                                                <PiCopy />
                                            </Button>
                                            <Button
                                                variant="outline"
                                                size="icon-sm"
                                                className="hover:text-primary"
                                                onClick={() => move("./" + directory.join("/"), file.name, file.type)}
                                            >
                                                <PiScissors />
                                            </Button>
                                            <Button
                                                variant="outline"
                                                size="icon-sm"
                                                className="hover:text-primary"
                                                onClick={() => openRenameDialog(file.name, file.type)}
                                                disabled={renaming}
                                            >
                                                {/* Place the dialog outside to prevent opening many dialogs at the same time*/}
                                                <PiNotePencil />
                                            </Button>
                                            <Button
                                                variant="outline"
                                                size="icon-sm"
                                                className="hover:text-destructive"
                                                onClick={() => deleteObject(file.name, file.type)}
                                                disabled={uploading}
                                            >
                                                <PiTrash />
                                            </Button>
                                        </div>
                                    </TableCell>
                                </TableRow>
                            ))
                        }
                    </TableBody>
                </Table>
                {
                    !editing ? <></> : (
                        <InstanceFileEditor 
                            className="min-w-0 max-w-full" 
                            id={id} 
                            directory={editingDirectory}
                            name={editingName}
                            closeCallback={() => setEditing(false)}
                        />
                    )
                }
            </div>
            <Dialog open={openRename} onOpenChange={setOpenRename}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Rename</DialogTitle>
                    </DialogHeader>
                    <FieldGroup>
                        <Field>
                            <FieldLabel>New Name</FieldLabel>
                            <Input value={newName}
                                   onChange={(event) => setNewName(event.target.value)}
                                   type="text" />
                        </Field>
                    </FieldGroup>
                    <DialogFooter>
                        <DialogClose render={<Button variant="outline">Cancel</Button>} />
                        <Button
                            type="submit"
                            onClick={rename}
                            disabled={renaming}
                        >
                            Rename
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
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