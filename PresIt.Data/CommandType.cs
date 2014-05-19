using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public enum CommandType {
        [EnumMember(Value = "None")] None,
        [EnumMember(Value = "Error")] Error,
        [EnumMember(Value = "NextSlide")] NextSlide,
        [EnumMember(Value = "PreviousSlide")] PreviousSlide
    }
}