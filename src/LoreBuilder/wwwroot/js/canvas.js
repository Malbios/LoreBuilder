// Converts a viewport-relative (clientX/clientY) point into a point relative to the given
// scrollable element's own content, accounting for its current scroll position - used to place
// a newly-created lore cluster where a card was actually dropped on the canvas.
window.loreBuilderCanvas = {
    toContentRelative: function (element, clientX, clientY) {
        const rect = element.getBoundingClientRect();
        return {
            x: clientX - rect.left + element.scrollLeft,
            y: clientY - rect.top + element.scrollTop
        };
    }
};
