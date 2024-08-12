const productsAsyncAPI = require('./ProductAPIAsync.js');
const report = require('../Helper/MetricHelper.js');

/**
 * @param {Object} ObjectReq The object of the request retrieved from log json struct:{url,requestBodyText,reqType}
*/
export function replayRequest(ObjectReq) {
    switch (ObjectReq.requestMethod) {
        case 'POST': //Hit Post request
            let reqTypeName = productsAsyncAPI.getRequestTypeName(ObjectReq.url);
            var group_duration = report.getGroupDuration(reqTypeName, () => {
                productsAsyncAPI.resendRequest(ObjectReq.url, ObjectReq.requestBodyText);
            });
            report.manageDuration(group_duration, reqTypeName);
            break;

        case 'GET': // Hit Get request
            productsAPI.replayGetRequest(ObjectReq.url);
            break;
    }
}