window.onload = function () {
	let webSocket;
	const wsStatus = document.getElementById("status");
	const wsSerialNo = document.getElementById("ws-serial-no");
	const wsIpPort = document.getElementById("ws-ip-port");
	const wsRegCode = document.getElementById("ws-reg-code");
	const wsAckMsg = document.getElementById("ws-ackmsg");
	const wsUl = document.getElementById("ws-ul");
	const wsConnectBtn = document.getElementById("ws-btn-connect");
	const wsTestBtn = document.getElementById("ws-btn-test");
	const wsClearBtn = document.getElementById("ws-btn-clear");
	const wsHeartBeat = document.querySelectorAll("body .fa-heart");
	const accessLogCardSN = document.querySelector("#ws-accesslog-card-sn");
	const accessLogStatus = document.querySelector("#in-out-status");
	const accessLogButton = document.querySelector("#btn-accesslog");
	const accessLogsButton = document.querySelector("#btn-accesslogs");
	const getAccessRightButton = document.querySelector("#btn-get-access-right");
	const getInOutStatusButton = document.querySelector("#btn-get-in-out-status");
	const employeeId = document.querySelector("#ws-employee-id");
	const originalId = document.querySelector("#ws-employee-original-id");
	const employeeCardSN = document.querySelector("#ws-employee-card-sn");
	const employeePassword = document.querySelector("#ws-employee-password");
	const getStatusByIdButton = document.querySelector('#btn-get-status-by-id');
	const requestPermissionButton = document.querySelector('#btn-request-permission');
	const getEmployeeButton = document.querySelector('#btn-employee-get');
	const addEmployeeButton = document.querySelector('#btn-employee-add');
	const updateEmployeeButton = document.querySelector('#btn-employee-update');
	const deleteEmployeeButton = document.querySelector('#btn-employee-delete');
	const getDepartmentButton = document.querySelector('#btn-department-get');
	const addDepartmentButton = document.querySelector('#btn-department-add');
	const updateDepartmentButton = document.querySelector('#btn-department-update');
	const terminalListButton = document.querySelector('#btn-terminal-list');
	const timeslotButton = document.querySelector('#btn-timeslot');

	// get/set fake sn @localStorage (201105)
	var storageSN = wsSerialNo.value = window.localStorage.getItem("sn");
	if (!wsSerialNo.value) {
		wsSerialNo.value = "7100-" + Math.floor(Math.random() * (8999) + 1000) + "-" + Math.floor(Math.random() * (8999) + 1000);
		window.localStorage.setItem("sn", wsSerialNo.value);
	}

	// ditto (201105)
	var storageIpPort = wsIpPort.value = window.localStorage.getItem("ipPort");
	if (!wsIpPort.value) {
		wsIpPort.value = "localhost:50595";
		window.localStorage.setItem("ipPort", wsIpPort.value);
	}

	// ditto (221014)
	var storageRegCode = wsRegCode.value = window.localStorage.getItem("regCode");
	if (!wsRegCode.value) {
		wsRegCode.value = "123456";
		window.localStorage.setItem("regCode", wsRegCode.value);
	}

	var storageTerminalList = window.localStorage.getItem("terminalList");
	if (storageTerminalList) {
		var optionsArray = JSON.parse(storageTerminalList);
		var checkboxes = document.querySelectorAll('#terminalDropdown input[type="checkbox"]');
		checkboxes.forEach(checkbox => {
			if (optionsArray.includes(checkbox.value)) {
				checkbox.checked = true;
			}
		});
	}

	var storageTimeslotList = window.localStorage.getItem("selectedTimeslot");
	if (storageTimeslotList) {
		var optionsArray = JSON.parse(storageTimeslotList);
		var radioButtons = document.querySelectorAll("#timeslotDropdown input[type='radio']");
		radioButtons.forEach(radio => {
			if (optionsArray.includes(radio.value)) {
				radio.checked = true;
			}
		});
	}

	// setup smartCard SN dropdown (210127)
	const setSmartCardSNDropdown = (snArray) => {
		snArray.forEach(sn => {
			var option = document.createElement('option');
			option.innerHTML = "0x" + sn.toString(16).toUpperCase().padStart(10, '0');
			option.value = sn;
			accessLogCardSN.add(option);
			employeeCardSN.add(option.cloneNode(true));
		});
	};

	const onConnected = () => {
		accessLogButton.disabled = false;
		accessLogsButton.disabled = false;
		getAccessRightButton.disabled = false;
		getInOutStatusButton.disabled = false;
		getStatusByIdButton.disabled = false;
		requestPermissionButton.disabled = false;
		getEmployeeButton.disabled = false;
		addEmployeeButton.disabled = false;
		updateEmployeeButton.disabled = false;
		deleteEmployeeButton.disabled = false;
		wsTestBtn.disabled = false;
		getDepartmentButton.disabled = false;
		addDepartmentButton.disabled = false;
		updateDepartmentButton.disabled = false;
		terminalListButton.disabled = false;
		timeslotButton.disabled = false;
	}

	const onDisconnected = () => {
		accessLogButton.disabled = true;
		accessLogsButton.disabled = true;
		getAccessRightButton.disabled = true;
		getInOutStatusButton.disabled = true;
		getStatusByIdButton.disabled = true;
		requestPermissionButton.disabled = true;
		getEmployeeButton.disabled = true;
		addEmployeeButton.disabled = true;
		updateEmployeeButton.disabled = true;
		deleteEmployeeButton.disabled = true;
		wsTestBtn.disabled = true;
		getDepartmentButton.disabled = true;
		addDepartmentButton.disabled = true;
		updateDepartmentButton.disabled = true;
		terminalListButton.disabled = true;
		timeslotButton.disabled = true;
	}

	const connectServer = async () => {
		try {
			// for the browser to connect to the app via websocket, not connecting to iGuardPayroll (comment (221007))
			wsStatus.innerHTML = "Connecting...";
			const wsUrl = "wss://" + location.host;
			webSocket = new WebSocket(wsUrl);
			webSocket.onopen = function (event) {
				wsStatus.innerHTML = "ONLINE!";
			}
			webSocket.onmessage = function (event) {
				let obj;
				try {
					obj = JSON.parse(event.data);
					if (obj) {
						if (obj.eventType === 'onConnected') {
							wsAckMsg.innerHTML = obj.data[0];
							wsAckMsg.classList.remove('alert');
							onConnected();
							wsUl.innerHTML = '';
						} else if (obj.eventType === 'onConnecting') {
							wsAckMsg.innerHTML = obj.data[0];
							wsAckMsg.classList.remove('alert');
							setSmartCardSNDropdown(obj.data[1]);
						} else if (obj.eventType === 'onReconnecting') {
							wsAckMsg.classList.remove('alert');
							wsAckMsg.innerHTML = obj.data[0];
						} else if (obj.eventType === 'onError') {
							wsAckMsg.innerHTML = obj.data[0];
							if (wsAckMsg.innerHTML === "The server returned status code '404' when status code '101' was expected.") wsAckMsg.innerHTML += " (orphan?)";
							wsAckMsg.classList.add('alert');
							wsConnectBtn.disabled = false;
						} else if (obj.eventType === 'heartBeat') {
							wsHeartBeat[0].style.display = "block";
							setTimeout(() => {
								wsHeartBeat[0].style.display = "none"
							}, 400);
						} else if (obj.eventType === 'onGetEmployee') {
							document.querySelector('#ws-employee-lastname').value = obj.data[0];
							document.querySelector('#ws-employee-firstname').value = obj.data[1];
							document.querySelector('#ws-employee-card-sn').value = obj.data[2];
							document.querySelector('#is-active').value = obj.data[3];
						} else {
							// either Tx or Rx (comment (221014))
							var data = JSON.parse(obj.data);
							if (obj.eventType === 'Rx' && ['RegistrationFailed', 'UnRegistered'].includes(data?.eventType)) {
								wsAckMsg.innerHTML = data.data[0];
								wsAckMsg.classList.add('alert');
								wsConnectBtn.disabled = false;
								onDisconnected();
							}
							wsUl.innerHTML += '<li>' + '<span class="ws-name">' + obj.eventType + '</span>' + ': ' + obj.data + '</li>';
						}
					}
				} catch {
					wsUl.innerHTML += '<li><span class="ws-name">catch</span>: ' + event.data + '</li>';
				}
			}
			webSocket.onclose = async function (event) {
				wsStatus.innerHTML = "WebSocket Closed (code:" + event.code + ", reason:" + event.reason + ")!";
				await new Promise(r => setTimeout(r, 2000));
				connectServer();
			}
		} catch (error) {
			wsStatus.innerHTML = error;
			await new Promise(r => setTimeout(r, 2000));
			connectServer();
		}
	};

	connectServer();

	wsConnectBtn.onclick = () => {
		wsUl.innerHTML = null;
		wsConnectBtn.disabled = true;
		if (storageIpPort !== wsIpPort.value) {
			storageIpPort = wsIpPort.value;
			window.localStorage.setItem("ipPort", wsIpPort.value);
		}
		if (storageSN !== wsSerialNo.value) {
			storageSN = wsSerialNo.value;
			window.localStorage.setItem("sn", wsSerialNo.value);
		}
		if (storageRegCode !== wsRegCode.value) {
			storageRegCode = wsRegCode.value;
			window.localStorage.setItem("regCode", wsRegCode.value);
		}
		const jsonObj = {
			eventType: "onConnectClick",
			data: [{
				SN: wsSerialNo.value,
				IpPort: wsIpPort.value,
				RegCode: wsRegCode.value.toUpperCase()
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	wsClearBtn.onclick = () => {
		wsUl.innerHTML = "";
	}

	accessLogButton.onclick = () => {
		const jsonObj = {
			eventType: "accessLog",
			data: [{
				cardSN: accessLogCardSN.value,
				status: accessLogStatus.value
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	accessLogsButton.onclick = () => {
		const statusArray = Array.from(accessLogStatus.options).map((e) => {
			return e.value;
		});
		const data = Array.from(accessLogCardSN.options).map((e) => {
			return {
				cardSN: e.value,
				status: statusArray[Math.floor(Math.random() * statusArray.length)]
			}
		});
		const jsonObj = {
			eventType: "accessLogs",
			data: data
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	getAccessRightButton.onclick = () => {
		const jsonObj = {
			eventType: "GetAccessRight",
			data: [{
				smartCardSN: parseInt(accessLogCardSN.value)
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	getInOutStatusButton.onclick = () => {
		const jsonObj = {
			eventType: "GetInOutStatus",
			data: [{
				smartCardSN: parseInt(accessLogCardSN.value)
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	getStatusByIdButton.onclick = () => {
		const jsonObj = {
			eventType: "GetInOutStatus",
			data: [{
				smartCardSN: 0,
				employeeId: employeeId.value
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	requestPermissionButton.onclick = () => {
		const jsonObj = {
			eventType: "RequestPermission",
			data: [{
				employeeId: employeeId.value
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	getEmployeeButton.onclick = () => {
		const jsonObj = {
			eventType: "GetEmployee",
			data: [{
				employeeId: employeeId.value == '' ? null : employeeId.value.toUpperCase(),
				smartCardSN: employeeCardSN.value == '' ? null : employeeCardSN.value
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	/* add & update department (240222) */
	getDepartmentButton.onclick = () => {
		const jsonObj = {
			eventType: "GetDepartment",
			data: [{
				departmentId: document.querySelector('#ws-department-id').value.toUpperCase()
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);

		// debug
		console.log("TP#05 jsonStr:", jsonStr);

		webSocket.send(jsonStr);
	}

	addDepartmentButton.onclick = () => {
		const checkboxes = document.querySelectorAll('#terminalDropdown input[type="checkbox"]');
		let selectedOptions = [];
		checkboxes.forEach(checkbox => {
			if (checkbox.checked) {
				selectedOptions.push(checkbox.value);
			}
		});
		window.localStorage.setItem("terminalList", JSON.stringify(selectedOptions));

		var selectedTimeslot = document.querySelector('#timeslotDropdown input[type="radio"]:checked');
		if (selectedTimeslot) {
			localStorage.setItem('selectedTimeslot', JSON.stringify(selectedTimeslot.value));
		}

		const jsonObj = {
			eventType: "AddDepartment",
			data: [{
				departmentId: document.querySelector('#ws-department-id').value.toUpperCase(),
				departmentName: document.querySelector('#ws-department-name').value,
				terminals: selectedOptions,
				timeslot: selectedTimeslot ? selectedTimeslot.value : null
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	/* add & update employee */
	addEmployeeButton.onclick = () => setEmployee("AddEmployee");
	updateEmployeeButton.onclick = () => setEmployee("UpdateEmployee");

	var setEmployee = (eventtype) => {
		const jsonObj = {
			eventType: eventtype,
			data: [{
				employeeId: employeeId.value.toUpperCase(),
				originalId: originalId.value === '' ? null : originalId.value.toUpperCase(),
				smartCardSN: employeeCardSN.value === '' ? null : employeeCardSN.value,
				lastName: document.querySelector('#ws-employee-lastname').value,
				firstName: document.querySelector('#ws-employee-firstname').value,
				isActive: document.querySelector('#is-active').value,
				password: employeePassword.value === '' ? null : employeePassword.value
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	deleteEmployeeButton.onclick = () => {
		const jsonObj = {
			eventType: "DeleteEmployee",
			data: [{
				employeeId: employeeId.value.toUpperCase()
			}]
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}

	wsTestBtn.onclick = () => {
		const jsonObj = {
			eventType: "onTestClick",
			data: []
		}
		const jsonStr = JSON.stringify(jsonObj);
		webSocket.send(jsonStr);
	}
}

function openTab(element, tabName) {
	var i, tabcontent, tablinks;

	tabcontent = document.getElementsByClassName("tabcontent");
	for (i = 0; i < tabcontent.length; i++) {
		tabcontent[i].style.display = "none";
	}

	tablinks = document.getElementsByClassName("tablinks");
	for (i = 0; i < tablinks.length; i++) {
		tablinks[i].className = tablinks[i].className.replace(" active", "");
	}

	document.getElementById(tabName).style.display = "block";
	element.className += " active";

	setCookie("lastOpenedTab", tabName, 30);
}

function getCookie(name) {
	var nameEQ = name + "=";
	var ca = document.cookie.split(';');
	for (var i = 0; i < ca.length; i++) {
		var c = ca[i];
		while (c.charAt(0) == ' ') c = c.substring(1, c.length);
		if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
	}
	return null;
}

function setCookie(name, value, days) {
	var expires = "";
	if (days) {
		var date = new Date();
		date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
		expires = "; expires=" + date.toUTCString();
	}
	document.cookie = name + "=" + (value || "") + expires + "; path=/";
}

document.addEventListener("DOMContentLoaded", (event) => {
	var lastOpenedTab = getCookie("lastOpenedTab");
	if (lastOpenedTab) {
		var tabToOpen = document.querySelector(".tablinks[data-tab-name='" + lastOpenedTab + "']");
		if (tabToOpen) {
			tabToOpen.click();
		}
	} else {
		// if no cookie, just click the first tab
		document.getElementsByClassName("tablinks")[0].click();
	}
	document.getElementById('btn-terminal-list').addEventListener('click', function (event) {
		toggleDropdown(event, "terminalDropdown");
	});
	document.getElementById('btn-timeslot').addEventListener('click', function (event) {
		toggleDropdown(event, 'timeslotDropdown');
	});
});

function toggleDropdown(event, element) {
	const dropdownContent = document.getElementById(element);
	let isShowing = dropdownContent.classList.contains("show");
	closeAllDropdowns();
	if (!isShowing) {
		dropdownContent.classList.add("show");
		const hasDropdownOption = dropdownContent.classList.contains("dropdown-option-applied");
		if (!hasDropdownOption) {
			const children = dropdownContent.querySelectorAll("*");
			children.forEach(child => {
				child.classList.add("dropdown-option");
			});
			dropdownContent.classList.add("dropdown-option-applied");
		}
	}
	event.stopPropagation();
}

// close the dropdown if the user clicks outside of it (240223)
window.onclick = function (event) {
	if (!event.target.matches('.dropdown-option')) {
		closeAllDropdowns();
	}
}

function closeAllDropdowns() {
	var dropdowns = document.querySelectorAll(".dropdown-content");
	dropdowns.forEach(dropdown => {
		dropdown.classList.remove('show');
	});
}
