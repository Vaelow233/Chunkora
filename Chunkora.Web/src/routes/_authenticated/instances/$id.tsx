import {createFileRoute, Link} from '@tanstack/react-router'
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {getInstance, killInstance, startInstance, stopInstance} from "@/api/instance.ts";
import {useEffect, useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbPage,
    BreadcrumbSeparator
} from "@/components/ui/breadcrumb.tsx";
import {Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {Button} from "@/components/ui/button.tsx";
import {InstanceOverview} from "@/components/instances/InstanceOverview.tsx";
import {InstanceStatusBadge} from "@/components/instances/InstanceStatusBadge.tsx";
import {InstanceFileManager} from "@/components/instances/InstanceFileManager.tsx";
import {PiChartLine, PiCube, PiFolder, PiGear, PiPause, PiPlay, PiSkull} from "react-icons/pi";
import {InstanceSettings} from "@/components/instances/InstanceSettings.tsx";

export const Route = createFileRoute('/_authenticated/instances/$id')({
  component: InstanceDetailPage,
})

function InstanceDetailPage() {
    const queryClient = useQueryClient()
    
    const [starting, setStarting] = useState(false)
    const [stopping, setStopping] = useState(false)
    const [killing, setKilling] = useState(false)
    
    const { id } = Route.useParams()
    
    const instanceQuery = useQuery({
        queryKey: ["instance", id],
        queryFn: () => getInstance(id),
        refetchInterval: 10000,
    })

    useEffect(() => {
        if (!instanceQuery.error) {
            return
        }

        toast.add({
            type: "error",
            title: `Failed to fetch the data of instance ${id}.`,
            description: instanceQuery.error.message,
        })
    }, [instanceQuery.error])

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: ["instance", id],
        })
    }
    
    const start = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to start instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setStarting(true)
        try {
            await startInstance(id)
            toast.add({
                type: "success",
                title: `Start instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to start instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setStarting(false)
        }
    }

    const stop = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to stop instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setStopping(true)
        try {
            await stopInstance(id)
            toast.add({
                type: "success",
                title: `Stop instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to stop instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setStopping(false)
        }
    }

    const kill = async (id: string) => {
        if (!id) {
            toast.add({
                type: "error",
                title: "Failed to kill instance.",
                description: "The id of the instance should be non-empty.",
            })
            return
        }
        setKilling(true)
        try {
            await killInstance(id)
            toast.add({
                type: "success",
                title: `Kill instance ${name} successfully.`,
            })
            await refresh()
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to kill instance.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setKilling(false)
        }
    }
    
    return (
      <div className="m-8">
          <Breadcrumb>
              <BreadcrumbList>
                  <BreadcrumbItem>
                      <BreadcrumbLink render={<Link to="/instances"/>}>Instances</BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator />
                  <BreadcrumbItem>
                      <BreadcrumbPage>{instanceQuery.data?.name ?? id}</BreadcrumbPage>
                  </BreadcrumbItem>
              </BreadcrumbList>
          </Breadcrumb>
          <div className="flex flex-row mt-4 gap-4 items-center">
              <div className="w-16 h-16 bg-primary-foreground flex justify-center items-center rounded-lg">
                  <PiCube className="w-12 h-12 text-primary" />
              </div>
              <div className="flex flex-col">
                  <div className="flex flex-row items-center gap-2">
                      <div className="text-2xl font-bold">{instanceQuery.data?.name}</div>
                      <InstanceStatusBadge status={instanceQuery.data?.status ?? 0} />
                      <div className="text-muted-foreground text-sm">Description: {instanceQuery.data?.description}</div>
                  </div>
                  <div>ID: {instanceQuery.data?.id}</div>
              </div>
              <div className="ml-auto shrink-0">
                  {
                      instanceQuery.data && instanceQuery.data.status === 0 ? (
                          <Button
                              className="ml-6 w-32"
                              variant="outline"
                              onClick={() => start(instanceQuery.data.id)} disabled={starting}
                          >
                              <PiPlay />
                              Start
                          </Button>
                      ) : <></>
                  }
                  {
                      instanceQuery.data && instanceQuery.data.status === 2 ? (
                          <Button
                              className="ml-6 w-32"
                              variant="outline"
                              onClick={() => stop(instanceQuery.data.id)} disabled={stopping}
                          >
                              <PiPause />
                              Stop
                          </Button>
                      ) : <></>
                  }
                  {
                      instanceQuery.data && instanceQuery.data.status !== 0 ? (
                          <Button
                              className="ml-6 w-32"
                              variant="destructive"
                              onClick={() => kill(instanceQuery.data.id)} disabled={killing}
                          >
                              <PiSkull />
                              Kill
                          </Button>
                      ) : <></>
                  }
              </div>
          </div>
          <Tabs defaultValue="overview">
              <TabsList variant="line" className="gap-6 mt-4">
                  <TabsTrigger value="overview" className="text-lg">
                      <PiChartLine className="min-w-5 min-h-5" />
                      Overview
                  </TabsTrigger>
                  <TabsTrigger value="file-manager" className="text-lg">
                      <PiFolder className="min-w-5 min-h-5" />
                      File Manager
                  </TabsTrigger>
                  <TabsTrigger value="settings" className="text-lg">
                      <PiGear className="min-w-5 min-h-5" />
                      Settings
                  </TabsTrigger>
              </TabsList>
              <TabsContent value="overview" className="mt-4">
                  <InstanceOverview id={id} instance={instanceQuery.data} />
              </TabsContent>
              <TabsContent value="file-manager" className="mt-4">
                  <InstanceFileManager id={id} />
              </TabsContent>
              <TabsContent value="settings" className="mt-4">
                  <InstanceSettings id={id} instance={instanceQuery.data} />
              </TabsContent>
          </Tabs>
      </div>
    )
}
