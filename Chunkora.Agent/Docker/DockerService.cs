using Chunkora.Contracts.Agent.V1;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Chunkora.Agent.Docker;

public class DockerService(DockerClient docker)
{
    public async Task CreateContainer(Instance instance)
    {
        var response = await docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = instance.Id
        });
    }
}