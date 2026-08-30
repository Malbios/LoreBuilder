// Converts a viewport-relative (clientX/clientY) point into a point relative to the given
// scrollable element's own content, accounting for its current scroll position - used to place
// a newly-created lore cluster where a card was actually dropped on the canvas.
window.loreBuilderCanvas = {
    // Returns a plain [x, y] array rather than an object - System.Text.Json (as used
    // internally by Blazor's JS interop) can deserialize an array straight into a .NET array
    // with no constructor-resolution involved, unlike a custom object shape.
    toContentRelative: function (element, clientX, clientY) {
        const rect = element.getBoundingClientRect();
        return [
            clientX - rect.left + element.scrollLeft,
            clientY - rect.top + element.scrollTop
        ];
    }
};
