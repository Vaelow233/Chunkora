using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class InstanceErrors
{
    public static readonly Error NameExists = 
        new(40001, "instance.name_exists", "An instance with a same name already exists");
    
    public static readonly Error NotFound =
        new(40002, "instance.not_found", "Instance not found");

    public static readonly Error FailedToListDirectoryObjects = 
        new(41001, "instance.io.failed_to_list_directory_objects", "Failed to list instance directory objects");
    
    public static readonly Error FailedToReadFile = 
        new(41002, "instance.io.failed_to_read_file", "Failed to read file");
    
    public static readonly Error FailedToReadLogs = 
        new(41003, "instance.io.failed_to_read_logs", "Failed to read logs");

    public static readonly Error FailedToCreateFile =
        new(41004, "instance.io.failed_to_create_file", "Failed to create file");
    
    public static readonly Error FailedToCreateDirectory =
        new(41005, "instance.io.failed_to_create_directory", "Failed to create directory");
    
    public static Error FailedToUploadFile(string detail = "") => 
        new(41006, "instance.io.failed_to_upload_file", $"Failed to upload file: {detail}");

    public static readonly Error UnknownContentLength =
        new(41007, "instance.io.unknown_content_length", "Unknown content length");
    
    public static Error FailedToDeleteFile(string detail = "") => 
        new(41008, "instance.io.failed_to_delete_file", $"Failed to delete file: {detail}");
    
    public static Error FailedToDeleteDirectory(string detail = "") => 
        new(41009, "instance.io.failed_to_delete_directory", $"Failed to delete directory: {detail}");
    
    public static Error FailedToCopyFile(string detail = "") => 
        new(41010, "instance.io.failed_to_copy", $"Failed to copy: {detail}");
    
    public static Error FailedToMoveFile(string detail = "") => 
        new(41011, "instance.io.failed_to_move", $"Failed to move: {detail}");
    
    public static Error FailedToRenameFile(string detail = "") => 
        new(41012, "instance.io.failed_to_rename", $"Failed to rename: {detail}");
}