using System.Runtime.Serialization;

namespace PresIt.Data {

    /// <summary>
    /// Represents a Command from the Server to the Presentation Client Application
    /// </summary>
    [DataContract]
    public enum CommandType {
        [EnumMember(Value = "None")] None,
        [EnumMember(Value = "Error")] Error,
        [EnumMember(Value = "NextSlide")] NextSlide,
        [EnumMember(Value = "PreviousSlide")] PreviousSlide,
        [EnumMember(Value = "Pause")] Pause
    }
}