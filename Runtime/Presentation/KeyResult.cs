namespace Artmann.Terminal
{
    public enum KeyResult
    {
        // Let the input field handle the key.
        Unhandled,

        // The presenter used the key. Stop it from reaching the input field.
        Handled,

        // Escape with nothing else to close. The view should drop input focus.
        Blur,

        // A line was submitted and the input cleared. The view should keep input focus.
        Submitted
    }
}
