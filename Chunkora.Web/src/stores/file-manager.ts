import {create} from "zustand";
import {persist} from "zustand/middleware";

interface FileManagerRecord {
    instanceId: string
    directories: string[]
}

interface FileManagerStore {
    records: FileManagerRecord[];
    update: (instanceId: string, directories: string[]) => void;
    get: (instanceId: string) => FileManagerRecord | undefined;
}

export const useFileManagerStore = create<FileManagerStore>()(
    persist(
        (set, get) => ({
            records: [],

            update: (instanceId: string, directories: string[]) => {
                const { records } = get()
                for (const record of records) {
                    if (record.instanceId === instanceId) {
                        record.directories = directories;
                        set({records})
                        return
                    }
                }
                set({
                    records: [...records, { instanceId: instanceId, directories: directories }]
                })
            },
            
            get: (instanceId: string) => {
                const { records } = get()
                for (const record of records) {
                    if (record.instanceId === instanceId) {
                        return record
                    }
                }
            },
        }),
        {
            name: "chunkora-web-auth",
        }
    ),
)