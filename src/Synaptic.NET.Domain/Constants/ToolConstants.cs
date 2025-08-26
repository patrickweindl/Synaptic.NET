namespace Synaptic.NET.Domain.Constants;

public static class ToolConstants
{
    public const string CreateMemoryToolName = "CreateMemory";
    public const string CreateMemoryToolTitle = "Create Memory";

    public const string GetMemoryToolName = "GetMemory";
    public const string GetMemoryToolTitle = "Acquire Memory";
    public const string GetMemoryToolDescription =
        "Search through all memories with a free text query, typically used to augment any response given with relevant contextual information to the user's query.";

    public const string GetStoreIdsAndDescriptionsToolName = "GetStoreIdentifiersAndDescriptions";
    public const string GetStoreIdsAndDescriptionsToolTitle = "Acquire Memory Store Identifiers and Descriptions";
    public const string GetStoreIdsAndDescriptionsToolDescription =
        "Retrieve memory store identifiers with short descriptions, useful for understanding the available memory stores and their contents.";

    public const string DeleteMemoryToolName = "DeleteMemory";
    public const string DeleteMemoryToolTitle = "Delete Memory";
    public const string DeleteMemoryToolDescription = "Deletes a memory from the memory stores.";

    public const string DeleteStoreToolName = "DeleteStore";
    public const string DeleteStoreToolTitle = "Delete Memory Store";
    public const string DeleteStoreToolDescription = "Deletes a memory store and all its memories.";

    public const string ReplaceMemoryToolName = "ReplaceMemory";
    public const string ReplaceMemoryToolTitle = "Replace Memory";
    public const string ReplaceMemoryToolDescription = "Replaces an existing memory with a new one. If the memory does not exist, it will be created.";

    public const string UpdateUserProfileToolName = "UpdateUserProfile";
    public const string UpdateUserProfileToolTitle = "Update User Profile";
    public const string UpdateUserProfileToolDescription = "Updates the user's profile, replacing any current information.";

    public const string UpdateUserFocusStateToolName = "UpdateUserFocusState";
    public const string UpdateUserFocusStateToolTitle = "Update User Focus State";
    public const string UpdateUserFocusStateToolDescription = "Updates the user's focus state, which can be used to tailor responses based on the user's current focus or attention level.";

    public const string GetTaskStoreIdentifiersAndTopicsToolName = "GetTaskStoreIdentifiersAndTopics";
    public const string GetTaskStoreIdentifiersAndTopicsToolTitle = "Acquire Task Store Identifiers and Topics";
    public const string GetTaskStoreIdentifiersAndTopicsToolDescription = "Retrieve task store identifiers with their topics.";

    public const string GetUpcomingTasksToolName = "GetUpcomingTasks";
    public const string GetUpcomingTasksToolTitle = "Acquire Upcoming Tasks";
    public const string GetUpcomingTasksToolDescription = "Retrieve all tasks scheduled for future dates.";

    public const string GetTodayTasksToolName = "GetTodayTasks";
    public const string GetTodayTasksToolTitle = "Acquire Today's Tasks";
    public const string GetTodayTasksToolDescription = "Retrieve all tasks scheduled for today.";

    public const string CreateNotificationToolName = "CreateNotification";
    public const string CreateNotificationToolTitle = "Create Notification";
    public const string CreateNotificationToolDescription = "Creates a notification within the task stores. If the store does not exist, it will be created automatically.";

    public const string CreateAsynchronousMessageToolName = "CreateAsynchronousMessage";
    public const string CreateAsynchronousMessageToolTitle = "Create Asynchronous Message";
    public const string CreateAsynchronousMessageToolDescription = "Creates a prompt within the task stores that will be executed through a LLM like GPT-5 with Web Search Access and access to memories at the scheduled date and the response sent to the user at the execution date. If the store does not exist, it will be created automatically.";

    public const string DeleteTaskToolName = "DeleteTask";
    public const string DeleteTaskToolTitle = "Delete Task";
    public const string DeleteTaskToolDescription = "Deletes a task from the task stores.";

    public const string DeleteTaskStoreToolName = "DeleteTaskStore";
    public const string DeleteTaskStoreToolTitle = "Delete Task Store";
    public const string DeleteTaskStoreToolDescription = "Deletes a task store and all tasks contained within it.";

    public const string ReplaceTaskToolName = "ReplaceTask";
    public const string ReplaceTaskToolTitle = "Replace Task";
    public const string ReplaceTaskToolDescription = "Replaces an existing task with a new one. If the task does not exist, it will be created.";

    public const string UpdateTaskStoreTopicToolName = "UpdateTaskStoreTopic";
    public const string UpdateTaskStoreTopicToolTitle = "Update Task Store Topic";
    public const string UpdateTaskStoreTopicToolDescription = "Updates the topic or description of an existing task store.";

    public const string RescheduleTaskToolName = "RescheduleTask";
    public const string RescheduleTaskToolTitle = "Reschedule Task";
    public const string RescheduleTaskToolDescription = "Reschedules a task to a new date and time.";

    public const string AddAssistantMemoryBehaviorToolName = "AddAssistantMemoryBehavior";
    public const string AddAssistantMemoryBehaviorToolTitle = "Add Memory Behavior";
    public const string AddAssistantMemoryBehaviorToolDescription = "Adds a memory behavior to the assistant's memory behavior rules. \r\nThis tool is used to add a specific memory behavior that the assistant should follow when processing memories.";

    public const string RemoveAssistantMemoryBehaviorToolName = "RemoveAssistantMemoryBehavior";
    public const string RemoveAssistantMemoryBehaviorToolTitle = "Remove Memory Behavior";
    public const string RemoveAssistantMemoryBehaviorToolDescription = "Removes a memory behavior from the assistant's memory behavior rules. \r\nThis tool is used to remove a specific memory behavior that has been previously added to the assistant's configuration.";

    public const string AddAssistantTaskBehaviorToolName = "AddAssistantTaskBehavior";
    public const string AddAssistantTaskBehaviorToolTitle = "Add Task Behavior";
    public const string AddAssistantTaskBehaviorToolDescription = "Adds a task behavior to the assistant's task behavior rules.";

    public const string RemoveAssistantTaskBehaviorToolName = "RemoveAssistantTaskBehavior";
    public const string RemoveAssistantTaskBehaviorToolTitle = "Remove Task Behavior";
    public const string RemoveAssistantTaskBehaviorToolDescription = "Removes a task behavior from the assistant's task behavior rules. \r\nThis tool is used to remove a specific task behavior that has been previously added to the assistant's configuration.";

    public const string UpdateAssistantPersonaToolName = "UpdateAssistantPersona";
    public const string UpdateAssistantPersonaToolTitle = "Update Assistant Name";
    public const string UpdateAssistantPersonaToolDescription = "Updates the assistant's persona name.";

    public const string UpdateAssistantToneToolName = "UpdateAssistantTone";
    public const string UpdateAssistantToneToolTitle = "Update Assistant Tone";
    public const string UpdateAssistantToneToolDescription = "Updates the assistant's tone.";

    public const string UpdateAssistantStyleToolName = "UpdateAssistantStyle";
    public const string UpdateAssistantStyleToolTitle = "Update Assistant Style";
    public const string UpdateAssistantStyleToolDescription = "Updates the assistant's expression style.";

    public const string UpdateAssistantExplainingStyleToolName = "UpdateAssistantExplainingStyle";
    public const string UpdateAssistantExplainingStyleToolTitle = "Update Assistant Explaining Style";
    public const string UpdateAssistantExplainingStyleToolDescription = "Updates the assistant's explaining style.";

    public const string UpdateAssistantSilenceBehaviorToolName = "UpdateAssistantSilenceBehavior";
    public const string UpdateAssistantSilenceBehaviorToolTitle = "Update Assistant Silence Behavior";
    public const string UpdateAssistantSilenceBehaviorToolDescription = "Updates how the assistant behaves when it has nothing to say.";

    public const string UpdateAssistantDisagreementStyleToolName = "UpdateAssistantDisagreementStyle";
    public const string UpdateAssistantDisagreementStyleToolTitle = "Update Assistant Disagreement Style";
    public const string UpdateAssistantDisagreementStyleToolDescription = "Updates the assistant's disagreement style.";

    public const string UpdateAssistantResponseLengthToolName = "UpdateAssistantResponseLength";
    public const string UpdateAssistantResponseLengthToolTitle = "Update Assistant Response Length";
    public const string UpdateAssistantResponseLengthToolDescription = "Updates the preferred response length of the assistant.";

    public const string UpdateAssistantStructureToolName = "UpdateAssistantStructure";
    public const string UpdateAssistantStructureToolTitle = "Update Assistant Structure";
    public const string UpdateAssistantStructureToolDescription = "Updates the structure of the assistant's responses.";

    public const string UpdateAssistantEmojiUsageToolName = "UpdateAssistantEmojiUsage";
    public const string UpdateAssistantEmojiUsageToolTitle = "Update Assistant Emoji Usage";
    public const string UpdateAssistantEmojiUsageToolDescription = "Updates how the assistant uses emojis in its responses.";

    public const string GetUserProfileToolName = "GetUserProfile";
    public const string GetUserProfileToolTitle = "Get User Profile";
    public const string GetUserProfileToolDescription = "Retrieves the current user's profile information.";

    public const string GetAssistantProfileToolName = "GetAssistantProfile";
    public const string GetAssistantProfileToolTitle = "Get Assistant Profile";
    public const string GetAssistantProfileToolDescription = "Retrieve the current assistant's profile information.";

    public const string GetAssistantMemoriesToolName = "GetAssistantMemories";
    public const string GetAssistantMemoriesToolTitle = "Get Assistant Memories";
    public const string GetAssistantMemoriesToolDescription = "Retrieve the current assistant's memories.";

    public const string GetCurrentlyRelevantMemoriesToolName = "GetCurrentlyRelevantMemories";
    public const string GetCurrentlyRelevantMemoriesToolTitle = "Acquire Currently Relevant Memories";
    public const string GetCurrentlyRelevantMemoriesToolDescription = "Retrieve the currently relevant memories for a conversation start.";

    public const string GetCurrentTimeToolName = "GetCurrentTime";
    public const string GetCurrentTimeToolTitle = "Get Current Time";
    public const string GetCurrentTimeToolDescription = "Returns the current user time in various formats. Mandatory to be used ahead of any time sensitive operations.";
}
