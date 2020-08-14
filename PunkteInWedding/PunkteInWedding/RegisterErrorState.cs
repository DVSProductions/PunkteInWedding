namespace System {
    /// <summary>
    /// <see cref="Enum"/> that reflects the possible error states during the registration
    /// </summary>
 public enum RegisterErrorState : byte { Success = (byte)'s', EmailFound = (byte)'e', NameFound = (byte)'n',Fail=(byte)'x' }
}
