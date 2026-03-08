var DataFerretBridgeLib = {
    DataFerret_SendBatch: function(urlPtr, bodyPtr, authPtr, callbackObj, callbackMethod) {
        var url = UTF8ToString(urlPtr);
        var body = UTF8ToString(bodyPtr);
        var auth = UTF8ToString(authPtr);
        var objName = UTF8ToString(callbackObj);
        var methodName = UTF8ToString(callbackMethod);

        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': auth
            },
            body: body,
            keepalive: true
        }).then(function(response) {
            SendMessage(objName, methodName, response.ok ? '1' : '0');
        }).catch(function(error) {
            console.error('[DataFerret] fetch error:', error);
            SendMessage(objName, methodName, '0');
        });
    },

    DataFerret_SendBeacon: function(urlPtr, bodyPtr) {
        var url = UTF8ToString(urlPtr);
        var body = UTF8ToString(bodyPtr);
        navigator.sendBeacon(url, new Blob([body], { type: 'application/json' }));
    }
};

mergeInto(LibraryManager.library, DataFerretBridgeLib);
