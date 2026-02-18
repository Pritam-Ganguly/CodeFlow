"use strict";

var notificationContainer = document.getElementById("notification_list");
var notificationBadge = document.getElementById("notification_count_badge");
var toastContainer = document.getElementById("toast-container");


var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notification")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("LogOutput", function (data) {
    console.log(data);
})
 
connection.on("updateNotifications", async function (notification) {
    await updateNotifications(notification);
    await updateNotificationbadge();
})

init();

connection.start();

async function init() {
    await getNotifications();
}

async function updateNotifications(notification) {
    toastContainer.insertAdjacentHTML("afterbegin", generateToastMessage(notification));
    if (notificationContainer.dataset.empty == 1) {
        notificationContainer.innerHTML = '';
    }
    notificationContainer.insertAdjacentHTML("beforeend", convertToHTML(notification));
    notificationContainer.dataset.empty == 0;

    const toastNode = document.querySelector(`.toast_${notification.id}`)
    new bootstrap.Toast(toastNode).show();
    await addNotificationItemEventListener();
}

function generateToastMessage(notification) {
    let html = '';

    html += `<div class="toast toast_${notification.id} align-items-center text-white bg-primary border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
        <div class="toast-body">
            ${notification.message}
        </div>
        <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    </div>`;

    return html;
}

async function getNotifications() {
    const url = "/notification/all";

    const response = await fetch(url, {
        method: 'GET'
    }).then(res => res.json())
        .catch(e => console.error(e));
    notificationContainer.innerHTML = "";
    if (Array.from(response).length == 0) {
        let html = '';
        html += `<li class="p-2">No new notification</li>`
        notificationContainer.insertAdjacentHTML("beforeend", html);
        notificationContainer.dataset.empty = 1;
    }
    else {
        response.map(r => {
            notificationContainer.insertAdjacentHTML("beforeend", convertToHTML(r));
        });
        notificationContainer.dataset.empty = 0;
    }

    await addNotificationItemEventListener();
    await updateNotificationbadge();
}

async function updateNotificationbadge() {
    const url = "/notification/count";

    const response = await fetch(url, {
        method: 'GET'
    }).then(res => res.json())
        .catch(e => console.error(e));
    if (response < 1) {
        notificationBadge.innerText = '';
    }
    else if (response > 9) {
        notificationBadge.innerText = '9+';
    }
    else {
        notificationBadge.innerText = response;
    }
}

function convertToHTML(notification) {
    let html = '';
    html += `<li class="notification_item list-group-item dropdown-item p-2"
              data-questionid="${notification.questionId}" data-notificationid="${notification.id}">
             <i class="fa-solid fa-${getNotificationIcon(notification.check)}"></i><span class="p-2">${notification.message}</span><li>`;
    return html;
}

function getNotificationIcon(text) {
    switch (text) {
        case "accept":
            return "check";
        default:
            return "circle-info";
    }
    return 'info';
}

async function addNotificationItemEventListener() {
    document.querySelectorAll(".notification_item").forEach(n => {
        n.addEventListener("click", async function () {
            let questionId = this.dataset.questionid;
            let notficationId = this.dataset.notificationid;
            await markAsRead(notficationId);
            console.log(notficationId);
            window.location.href = `/Questions/details/${questionId}`;
        });
    });
}

async function markAsRead(notificationId) {
    const url = `/notification/mark_as_read/${notificationId}`;

    await fetch(url, {
        method: 'POST'
    }).then(res => res.json())
        .catch(e => console.error(e));
}