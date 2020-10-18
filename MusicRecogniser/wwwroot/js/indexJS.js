 
function closeVideo() {
    $('#PlayVideo').hide();
    $('#iFVideo').attr('src', "");
}
function PlayVideo(elem) {
    var id = $(elem).attr("id");
    $('#PlayVideo').show();
    $('#iFVideo').attr('src', "https://www.youtube.com/embed/" + id)
}
function Clear() {
    $("#Error").html("");
    $("#divVideos").empty();
    $('#MainVideo').css('visibility', 'hidden');
    $("#Btn_Search").attr("disabled", true);
    $('#divSearch').animate({ top: -150 }, 300);
    startUpdatingProgressIndicator();
}
function btn_SearchClick() {
    Clear();

    var data = {};
    data.URL = $("#txtURL").val();
    if (data.URL == "") {
        $("#txtURL").focus();
        return;
    }
    $.ajax({
        type: "POST",
        url: "/Home/Get_Videos",
        data: JSON.stringify(data),
        contentType: "application/json",
        dataType: "json",
        success: function (response) {
            if (response.success) {
                var NewSrt = data.URL;
                var URL = "";
                if (NewSrt.includes("youtu.be")) {
                    URL = NewSrt.replace("youtu.be", "www.youtube.com/embed");
                } else if (NewSrt.includes("watch?v=")) {
                    URL = NewSrt.replace("watch?v=", "embed/");
                }
                var JsonRes = JSON.stringify(response.response);
                var res = JSON.parse(JsonRes);

                for (var i = 0; i < res.length; i++) {
                    document.getElementById("divVideos").innerHTML += "<div class='col-sm-3' style='text-align:center;float:center;'> <img onclick='PlayVideo(this)' style='cursor: pointer;' id='" +
                        res[i].videoId + "' src='https://img.youtube.com/vi/" + res[i].videoId + "/default.jpg'>  <div style='font-size:14px;'>" + res[i].title + " </div> </div>   ";
                }
                $('#MainVideo').css('visibility', 'visible');
                $('#MainVideo').attr('src', URL)
                $('#myProgress').css('visibility', 'hidden');
                finish();
            } else {
                $("#Error").html("Sorry, the artist cannot be recognized");
                finish();
            }
        },
        error: function (response) {
            $("#Error").html("Sorry, the artist cannot be recognized");
            finish();
        }
    });
}

function finish() {
    stopUpdatingProgressIndicator();
    $("#Btn_Search").attr("disabled", false);
}

var intervalId;

function startUpdatingProgressIndicator() {
    $('#myProgress').css('visibility', 'visible');
    intervalId = setInterval(
        function () {
            $.ajax({
                type: "Get",
                url: "/Home/progress",
                contentType: "application/json",
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                }
            }).done(function (progress) {
                $("#myBar").css({ width: progress + "%" });
                $("#myBar").html(progress + "%");
            }
            );
        }
        , 20);
}

function stopUpdatingProgressIndicator() {
    clearInterval(intervalId);
}
 


