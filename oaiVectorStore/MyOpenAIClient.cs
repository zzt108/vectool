/*
 File format	MIME type
.c	text/x-c
.cpp	text/x-c++
.cs	text/x-csharp
.css	text/css
.doc	application/msword
.docx	application/vnd.openxmlformats-officedocument.wordprocessingml.document
.go	text/x-golang
.html	text/html
.java	text/x-java
.js	text/javascript
.json	application/json
.md	text/markdown
.pdf	application/pdf
.php	text/x-php
.pptx	application/vnd.openxmlformats-officedocument.presentationml.presentation
.py	text/x-python
.py	text/x-script.python
.rb	text/x-ruby
.sh	application/x-sh
.tex	text/x-tex
.ts	application/typescript
.txt	text/plain 
 */
using OpenAI;
namespace oaiVectorStore
{

    public class MyOpenAIClient:IDisposable
    {

        public MyOpenAIClient()
        {
            Client = new OpenAIClient();
        }

        public MyOpenAIClient(string apiKey)
        {
            Client = new OpenAIClient(apiKey);
        }

        public OpenAIClient Client { get; set; }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
