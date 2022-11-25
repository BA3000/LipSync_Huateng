import asyncio
from email import message
import websockets

async def echo(websocket, path):
    async for message in websocket:
        message = "copy that"
        await websocket.send(message)

if __name__ == "__main__":
    asyncio.get_event_loop().run_until_complete(websockets.serve(echo, 'localhost', 12345))
    asyncio.get_event_loop().run_forever()
