const productsAsyncAPI = require('./ProductAPIAsync.js');
const productsAPI = require('./ProductAPI.js');
const report = require('../helper/MetricHelper.js');

/**
 * @param {Object} ObjectReq The object of the request retrived from log json struct:{url,requestBodyText,reqType}
*/
export function ReplayRequest(ObjectReq) {
    switch (ObjectReq.requestMethod) {
        case 'POST': //Call helper for execution & reporting
            let reqTypeName = productsAPI.getRequestTypeName(ObjectReq.url);
            var group_duration = report.GetGroupDuration(reqTypeName, () => {
                productsAsyncAPI.resendRequest(ObjectReq.url, ObjectReq.requestBodyText);
            });
            report.manageDuration(group_duration, reqTypeName);
            break;

        case 'GET': // Call helper for execution & reporting
            productsAPI.replayGetRequest(ObjectReq.url);
            break;
    }
}