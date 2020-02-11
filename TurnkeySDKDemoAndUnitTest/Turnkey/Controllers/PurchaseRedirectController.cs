﻿using Turnkey.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Turnkey.exception;

namespace Turnkey.Controllers
{
    /// <summary>
    /// When user want to do purchase on cashier(not for mobile), user can refer to this class. 
    /// This class is mainly to get a purchase token, then open cashier page using the token for user purchasing.  Three steps:
    /// 1. Get token request paramters from httpContent object
    /// 2. Pass the request parameters to purchase token call method to execute the call
    /// 3. Get the purchase token, open cashier page(cashier url can be got from application configuration object) using the token
    /// </summary>
    public class PurchaseRedirectController : ApiController
    {
        public PurchaseRedirectController()
        {
        }

        public async Task<object> Post()

        {
            try
            {
                /* Get request paramters from HttpContent object, parse it to dictionary format*/
                HttpContent requestContent = Request.Content;
                string res = requestContent.ReadAsStringAsync().Result;
                Dictionary<String, String> inputParams = Tools.requestToDictionary(res);

                /*Init appliction configuration, get a config object*/
                string merchantID = Properties.Settings.Default.merchantId;
                string password = Properties.Settings.Default.password;
                string merchantNotificationUrl = Properties.Settings.Default.merchantNotificationUrl;
                string allowOriginUrl = Properties.Settings.Default.allowOriginUrl;
                string merchantLandingPageUrl = Properties.Settings.Default.merchantLandingPageUrl;
                string environment = Properties.Settings.Default.TurnkeySdkConfig;

                ApplicationConfig config = new ApplicationConfig(merchantID, password, allowOriginUrl, merchantNotificationUrl,
                                                                 merchantLandingPageUrl, environment);


                /*Execute the call and get the response*/
                Dictionary<String, String> executeData = new PurchaseTokenCall(config, inputParams).Execute();

                /*Get merchantID and token from the response, add them to input parameters, 
                  this input parameters will be used as cashier url parameters*/
                inputParams.Add("merchantId", config.MerchantId);
                inputParams.Add("token", executeData["token"]);

                /*Get cashier url from application configuration object, add parameters using inputparams*/
                String url = config.CashierUrl + "?";
                foreach (KeyValuePair<String, String> kvp in inputParams) {
                    url += kvp.Key + "=" + kvp.Value + "&";
                }
                url = url.Remove(url.Length - 1);

                /*Define a dictionary object, add the url to it, thus the dictionary object can be used by "CreateResponse" method which pass the url data to web page */
                Dictionary<String, String> response = new Dictionary<string, string>();
                response.Add("url", url);
                
               //Return the url data to web page
                return Request.CreateResponse(HttpStatusCode.OK, response);
                
            }
            catch (RequireParamException ex) {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing fields: " + ex.ToString());
            }
            catch (TokenAcquirationException ex) {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Could not acquire token: " + ex.ToString());
            }
            catch (PostToApiException ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Outgoing POST failed: " + ex.ToString());
            }
            catch (GeneralException ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "General SDK error: " + ex.ToString());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + ex.ToString());
            }
            
            
        }
    }
}
