window.dragDropHelper = {
    setData: function (e, data) {
        console.log("JS: setData")
        console.log(typeof(data))
        console.log(data)
        e.dataTransfer.setData("text/plain", data)
    },
    getData: function (e) {
        let x = e.dataTransfer.getData("text/plain")
        console.log("JS: getData")
        console.log(x)
        return x
    },
    allowDrop: function (e) {
        e.preventDefault()
    }
}
