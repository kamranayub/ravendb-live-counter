"use strict";

var connection = new signalR.HubConnectionBuilder()
  .withUrl("/counterHub")
  .build();

connection.on("UpdateCounter", function(counter) {
  document.getElementById("counter").innerText = counter;
});

connection
  .start()
  .then(function() {
    document.getElementById("connectionStatus").classList.add("online");
  })
  .catch(function(err) {
    document.getElementById("connectionStatus").classList.add("error");
    return console.error(err.toString());
  });