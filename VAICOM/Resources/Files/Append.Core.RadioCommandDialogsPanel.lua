-- Optimized VAICOM Append.Core.RadioCommandDialogsPanel.lua

-- Helper: Device safe accessor
local function getDeviceSafe(id)
	local device = base.GetDevice(id)
	return (device and device.get_frequency and device.get_modulation) and device or nil
end

-- Optimized: checkRadioCommunicatorTuned
function checkRadioCommunicatorTuned(target, communicator, communicatorId)
	local device = getDeviceSafe(communicatorId)
	if not device or not base.GetDevice(data.intercomId):is_communicator_available(communicatorId) then
		return false
	end

	local freqModTbl = target:getFrequenciesModulations()
	if not freqModTbl then return false end

	local radiofreq = device:get_frequency()
	local radiomod = device:get_modulation()

	for _, freqMod in base.pairs(freqModTbl) do
		if (radiomod == freqMod.modulation) and (base.math.abs(radiofreq - freqMod.frequency) < 2500) then
			return true
		end
	end
	return false
end

-- Optimized: checkRadioCommunicatorAvailability
function checkRadioCommunicatorAvailability(target, communicator, communicatorId)
	if not target then return true end
	local freqModTbl = target:getFrequenciesModulations()
	if not freqModTbl then return false end

	local intercom = base.GetDevice(data.intercomId)
	if not intercom or not intercom.is_communicator_available then return false end

	for _, freqMod in base.pairs(freqModTbl) do
		if checkCommunicator(communicator, freqMod.frequency, freqMod.modulation) and intercom:is_communicator_available(communicatorId) then
			return true
		end
	end
	return false
end

-- Optimized: selectCommunicatorDeviceId
function selectCommunicatorDeviceId(targetCommunicator)
	if not data.intercomId or not data.communicators then return nil end
	if not targetCommunicator then return data.intercomId end

	local function isValidCommunicator(id, communicator)
		local device = base.GetDevice(id)
		return device and device.is_on and device:is_on()
	end

	if data.curCommunicatorId == COMMUNICATOR_VOID or data.curCommunicatorId == COMMUNICATOR_AUTO or data.curCommunicatorId == 0 then
		for id, comm in base.pairs(data.communicators) do
			if isValidCommunicator(id, comm) and checkRadioCommunicatorTuned(targetCommunicator, comm, id) then
				return id
			end
		end
		for id, comm in base.pairs(data.communicators) do
			if isValidCommunicator(id, comm) and checkRadioCommunicatorAvailability(targetCommunicator, comm, id) then
				return id
			end
		end
	else
		local id = data.curCommunicatorId
		if checkRadioCommunicatorAvailability(targetCommunicator, data.communicators[id], id) then
			return id
		end
	end

	return nil
end

-- Optimized: SetParameters
function SetParameters(recipientcomm)
	local params = {}
	if base.vaicom.state.activemessage.insert then
		table.insert(params, recipientcomm)
	end
	local extra = base.vaicom.state.activemessage.parameters
	if extra and type(extra) == "table" then
		for _, param in ipairs(extra) do
			table.insert(params, param)
		end
	end
	return params
end

-- Optimized: onMsgStart
function onMsgStart(pMessage, pRecepient, text)
	if not data.initialized then return end

	local sender = pMessage:getSender()
	local receiver = pMessage:getReceiver()
	local event = pMessage:getEvent()

	if not sender or not sender.id_ then return end
	commById[sender:tonumber()] = sender
	if receiver then commById[receiver:tonumber()] = receiver end

	if data.pComm == nil or pRecepient ~= data.pComm then return end

	local textColor = getMessageColor(sender, receiver, event)

	for _, handler in base.pairs(data.msgHandlers) do
		local internalEvent, receiverAsRecepient = handler:onMsg(pMessage, pRecepient)
		if internalEvent then
			self:onEvent(internalEvent, sender, receiver:tonumber(), receiverAsRecepient)
		end
	end

	self:onEvent(event, sender:tonumber(), receiver and receiver:tonumber())

	if receiver == data.pComm or sender == data.pComm then
		commandDialogsPanel.onMsgStart(self, sender:tonumber(), receiver and receiver:tonumber(), text, textColor)
	end

	local sendtbl = {
		domsg = true,
		pMsgSender = sender,
		pMsgReceiver = receiver,
		eventid = event,
		eventkey = base.vaicom.helper.messagekey(event),
		text = text,
		parameters = pMessage:getTable().parameters,
		speech = base.vaicom.state.currentspeech,
		fsm = tostring(base.fsm.state)
	}

	socket.try(base.vaicom.relay:send(JSON:encode(sendtbl)))
end

-- Optimized: onMsgFinish
function onMsgFinish(pMessage, pRecepient, text)
	if not data.initialized then return end

	local sender = pMessage:getSender()
	local receiver = pMessage:getReceiver()

	if sender then commById[sender:tonumber()] = sender end
	if receiver then commById[receiver:tonumber()] = receiver end

	if receiver == data.pComm or sender == data.pComm then
		commandDialogsPanel.onMsgFinish(self, sender:tonumber(), receiver and receiver:tonumber(), text)
	end

	local sendtbl = {
		domsg = false,
		fsm = tostring(base.fsm.state)
	}

	socket.try(base.vaicom.relay:send(JSON:encode(sendtbl)))
end

-- Optimized: vaicom_loop
local function vaicom_loop()
	if not (base.vaicom and base.vaicom.receiver and data.initialized and data.pUnit) then
		Gui.EnableHighSpeedUpdate(true)
		Gui.RemoveUpdateCallback(vaicom_loop)
		return
	end

	if RemoteInputs() then
		base.vaicom.flags.remote = true
		ProcessRemoteCommand()
	elseif base.vaicom.flags.remote then
		base.vaicom.flags.remote = false
	end
end

-- Optimized: init.start
function base.vaicom.init.start()
	Gui.SetupApplicationUpdateCallback()
	Gui.EnableHighSpeedUpdate(true)
	Gui.AddUpdateCallback(vaicom_loop)

	base.vaicom.sender = socket.try(socket.udp())
	socket.try(base.vaicom.sender:setpeername(base.vaicom.config.sendaddress, base.vaicom.config.sendport))
	socket.try(base.vaicom.sender:settimeout(base.vaicom.config.sendtimeout))

	base.vaicom.receiver = socket.try(socket.udp())
	socket.try(base.vaicom.receiver:setsockname(base.vaicom.config.receiveaddress, base.vaicom.config.receiveport))
	socket.try(base.vaicom.receiver:settimeout(base.vaicom.config.receivetimeout))

	base.vaicom.relay = socket.try(socket.udp())
	socket.try(base.vaicom.relay:setpeername(base.vaicom.config.relayaddress, base.vaicom.config.relayport))
	socket.try(base.vaicom.relay:settimeout(base.vaicom.config.relaytimeout))
end

-- Optimized: init.stop
function base.vaicom.init.stop()
	for _, sock in pairs({ "sender", "receiver", "relay" }) do
		local s = base.vaicom[sock]
		if s then
			socket.try(s:close())
			base.vaicom[sock] = nil
		end
	end
end

-- Optimized: helper functions
base.vaicom.helper.tablelength = function(tbl)
	if type(tbl) ~= "table" then return 0 end
	local count = 0
	for _ in pairs(tbl) do count = count + 1 end
	return count
end

base.vaicom.helper.mergetables = function(A, B)
	local result = {}
	if type(A) == "table" then
		for _, v in pairs(A) do table.insert(result, v) end
	end
	if type(B) == "table" then
		for _, v in pairs(B) do table.insert(result, v) end
	end
	return result
end

base.vaicom.helper.messagekey = function(id)
	for key, val in pairs(base.Message) do
		if val == id then return key end
	end
	return nil
end

base.vaicom.objects.localRadios = function()
	return (type(data.communicators) == "table" and next(data.communicators)) and data.communicators or {}
end

base.vaicom.filter.hasradio = function(units)
	local result = {}
	for _, unit in pairs(units or {}) do
		local comm = unit.getCommunicator and unit:getCommunicator()
		if comm and comm:hasTransiver() then
			table.insert(result, unit)
		end
	end
	return result
end
